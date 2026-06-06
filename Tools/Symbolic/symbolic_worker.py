#!/usr/bin/env python3
import json
import re
import sys
import time
from typing import Any, Dict, List, Optional

import sympy as sp
from sympy.parsing.sympy_parser import (
    convert_xor,
    implicit_multiplication_application,
    standard_transformations,
    parse_expr,
)

ENGINE_NAME = "sympy"
MAX_EXPRESSION_LENGTH = 1000
MAX_RESULT_LENGTH = 5000
MAX_VARIABLE_LENGTH = 32
MAX_ORDER = 50
SUPPORTED_OPERATIONS = {
    "simplify",
    "expand",
    "factor",
    "diff",
    "integrate",
    "limit",
    "solve",
    "series",
}

_ALLOWED_FUNC_NAMES = [
    "sin", "cos", "tan",
    "asin", "acos", "atan",
    "exp", "log", "sqrt", "Abs",
]
_ALLOWED_CONSTS = {
    "pi": sp.pi,
    "E": sp.E,
    "oo": sp.oo,
}

TRANSFORMATIONS = standard_transformations + (
    convert_xor,
    implicit_multiplication_application,
)


class WorkerError(Exception):
    def __init__(self, code: str, message: str):
        self.code = code
        self.message = message
        super().__init__(message)


def _error_result(operation: Optional[str], expression: Optional[str], code: str, message: str, elapsed_ms: int) -> Dict[str, Any]:
    return {
        "success": False,
        "operation": operation,
        "input": expression,
        "resultText": None,
        "resultLatex": None,
        "resultJson": None,
        "engine": ENGINE_NAME,
        "engineVersion": sp.__version__,
        "warnings": [],
        "errorCode": code,
        "errorMessage": message,
        "elapsedMs": elapsed_ms,
    }


def _ok_result(operation: str, expression: str, result_obj: Any, warnings: List[str], elapsed_ms: int) -> Dict[str, Any]:
    result_text = str(result_obj)
    result_latex = sp.latex(result_obj)

    if len(result_text) > MAX_RESULT_LENGTH or len(result_latex) > MAX_RESULT_LENGTH:
        raise WorkerError("result_too_long", f"Result exceeds max length {MAX_RESULT_LENGTH}.")

    return {
        "success": True,
        "operation": operation,
        "input": expression,
        "resultText": result_text,
        "resultLatex": result_latex,
        "resultJson": None,
        "engine": ENGINE_NAME,
        "engineVersion": sp.__version__,
        "warnings": warnings,
        "errorCode": None,
        "errorMessage": None,
        "elapsedMs": elapsed_ms,
    }


def _safe_symbol_name(name: str) -> bool:
    if not name or len(name) > MAX_VARIABLE_LENGTH:
        return False
    return re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", name) is not None


def _normalize_expr_text(expr: str) -> str:
    return expr.replace("^", "**").strip()


def _build_local_dict(variable_names: List[str], assumptions: Optional[Dict[str, Any]]) -> Dict[str, Any]:
    local_dict: Dict[str, Any] = {}

    for func_name in _ALLOWED_FUNC_NAMES:
        local_dict[func_name] = getattr(sp, func_name)

    local_dict["ln"] = sp.log

    for k, v in _ALLOWED_CONSTS.items():
        local_dict[k] = v

    assumptions = assumptions or {}
    real_flag = assumptions.get("real", None)
    positive_flag = assumptions.get("positive", None)

    for var_name in variable_names:
        if not _safe_symbol_name(var_name):
            raise WorkerError("unsafe_expression", f"Unsafe variable name: {var_name}")
        symbol_kwargs: Dict[str, Any] = {}
        if isinstance(real_flag, bool):
            symbol_kwargs["real"] = real_flag
        if isinstance(positive_flag, bool):
            symbol_kwargs["positive"] = positive_flag
        local_dict[var_name] = sp.Symbol(var_name, **symbol_kwargs)

    return local_dict


def _parse_math_expression(expr_text: str, local_dict: Dict[str, Any]) -> Any:
    try:
        return parse_expr(expr_text, local_dict=local_dict, transformations=TRANSFORMATIONS, evaluate=True)
    except Exception:
        raise WorkerError("parse_error", "Failed to parse expression.")


def _parse_required_expr(payload: Dict[str, Any], local_dict: Dict[str, Any]) -> Any:
    expression = payload.get("expression")
    if not isinstance(expression, str) or not expression.strip():
        raise WorkerError("missing_required_field", "Field 'expression' is required.")

    if len(expression) > MAX_EXPRESSION_LENGTH:
        raise WorkerError("expression_too_long", f"Expression exceeds max length {MAX_EXPRESSION_LENGTH}.")

    expr_text = _normalize_expr_text(expression)
    return _parse_math_expression(expr_text, local_dict)


def _parse_optional_expr(value: Any, field_name: str, local_dict: Dict[str, Any]) -> Any:
    if value is None:
        return None
    text = str(value).strip()
    if not text:
        raise WorkerError("parse_error", f"Field '{field_name}' is empty.")
    if len(text) > MAX_EXPRESSION_LENGTH:
        raise WorkerError("expression_too_long", f"Field '{field_name}' exceeds max length {MAX_EXPRESSION_LENGTH}.")
    text = _normalize_expr_text(text)
    return _parse_math_expression(text, local_dict)


def _parse_variable(payload: Dict[str, Any], required: bool = False) -> Optional[str]:
    variable = payload.get("variable")
    if variable is None or str(variable).strip() == "":
        if required:
            raise WorkerError("missing_required_field", "Field 'variable' is required.")
        return None

    variable_str = str(variable).strip()
    if not _safe_symbol_name(variable_str):
        raise WorkerError("unsafe_expression", "Invalid variable name.")

    return variable_str


def _parse_operation(payload: Dict[str, Any]) -> str:
    operation = payload.get("operation")
    if not isinstance(operation, str) or not operation.strip():
        raise WorkerError("missing_required_field", "Field 'operation' is required.")

    op = operation.strip().lower()
    if op not in SUPPORTED_OPERATIONS:
        raise WorkerError("unsupported_operation", f"Unsupported operation: {op}")

    return op


def _ensure_input_format(payload: Dict[str, Any], warnings: List[str]) -> None:
    raw_input_format = payload.get("inputFormat")
    if raw_input_format is None:
        input_format = "plain"
    else:
        input_format = str(raw_input_format).strip().lower()
    if input_format == "latex":
        raise WorkerError("parse_error", "LaTeX input is not supported in this stage.")
    if input_format not in {"plain", "sympy", ""}:
        warnings.append(f"Unsupported inputFormat '{input_format}', treated as plain.")


def _compute(payload: Dict[str, Any]) -> Dict[str, Any]:
    warnings: List[str] = []
    _ensure_input_format(payload, warnings)

    op = _parse_operation(payload)
    var_name = _parse_variable(payload, required=(op in {"diff", "integrate", "limit", "series"}))

    assumptions = payload.get("assumptions")
    if assumptions is not None and not isinstance(assumptions, dict):
        raise WorkerError("parse_error", "Field 'assumptions' must be an object.")

    variable_names = []
    if var_name:
        variable_names.append(var_name)

    local_dict = _build_local_dict(variable_names, assumptions)
    expression_value = payload.get("expression")
    if not isinstance(expression_value, str) or not expression_value.strip():
        raise WorkerError("missing_required_field", "Field 'expression' is required.")
    if len(expression_value) > MAX_EXPRESSION_LENGTH:
        raise WorkerError("expression_too_long", f"Expression exceeds max length {MAX_EXPRESSION_LENGTH}.")
    normalized_expression = _normalize_expr_text(expression_value)

    expr = None
    if op != "solve" or "=" not in normalized_expression:
        expr = _parse_required_expr(payload, local_dict)

    if op == "simplify":
        result_obj = sp.simplify(expr)

    elif op == "expand":
        result_obj = sp.expand(expr)

    elif op == "factor":
        result_obj = sp.factor(expr)

    elif op == "diff":
        symbol = local_dict[var_name]  # type: ignore[index]
        result_obj = sp.diff(expr, symbol)

    elif op == "integrate":
        symbol = local_dict[var_name]  # type: ignore[index]
        lower = payload.get("lower")
        upper = payload.get("upper")
        if lower is not None and upper is not None:
            lower_expr = _parse_optional_expr(lower, "lower", local_dict)
            upper_expr = _parse_optional_expr(upper, "upper", local_dict)
            result_obj = sp.integrate(expr, (symbol, lower_expr, upper_expr))
        else:
            result_obj = sp.integrate(expr, symbol)

    elif op == "limit":
        symbol = local_dict[var_name]  # type: ignore[index]
        point_raw = payload.get("point")
        if point_raw is None or str(point_raw).strip() == "":
            raise WorkerError("missing_required_field", "Field 'point' is required for limit.")
        point_expr = _parse_optional_expr(point_raw, "point", local_dict)
        result_obj = sp.limit(expr, symbol, point_expr)

    elif op == "solve":
        symbol_name = var_name if var_name else "x"
        if symbol_name not in local_dict:
            local_dict[symbol_name] = sp.Symbol(symbol_name)
        symbol = local_dict[symbol_name]

        expr_text = normalized_expression
        if "=" in expr_text:
            left_text, right_text = expr_text.split("=", 1)
            left_expr = _parse_math_expression(left_text.strip(), local_dict)
            right_expr = _parse_math_expression(right_text.strip(), local_dict)
            equation = sp.Eq(left_expr, right_expr)
            result_obj = sp.solve(equation, symbol)
        else:
            if expr is None:
                expr = _parse_math_expression(expr_text, local_dict)
            result_obj = sp.solve(expr, symbol)

    elif op == "series":
        symbol = local_dict[var_name]  # type: ignore[index]
        point_raw = payload.get("point", "0")
        point_expr = _parse_optional_expr(point_raw, "point", local_dict)

        order_raw = payload.get("order", 6)
        try:
            order = int(order_raw)
        except Exception:
            raise WorkerError("parse_error", "Field 'order' must be an integer.")

        if order <= 0 or order > MAX_ORDER:
            raise WorkerError("unsafe_expression", f"Field 'order' must be between 1 and {MAX_ORDER}.")

        result_obj = sp.series(expr, symbol, point_expr, order)

    else:
        raise WorkerError("unsupported_operation", f"Unsupported operation: {op}")

    expression_input = str(payload.get("expression", ""))
    return {
        "operation": op,
        "expression_input": expression_input,
        "result_obj": result_obj,
        "warnings": warnings,
    }


def main() -> None:
    started = time.perf_counter()
    raw = sys.stdin.read()

    operation: Optional[str] = None
    expression: Optional[str] = None

    try:
        payload = json.loads(raw)
        if not isinstance(payload, dict):
            raise WorkerError("parse_error", "Input JSON must be an object.")

        operation = payload.get("operation") if isinstance(payload.get("operation"), str) else None
        expression = payload.get("expression") if isinstance(payload.get("expression"), str) else None

        computed = _compute(payload)
        elapsed_ms = int((time.perf_counter() - started) * 1000)
        response = _ok_result(
            computed["operation"],
            computed["expression_input"],
            computed["result_obj"],
            computed["warnings"],
            elapsed_ms,
        )

    except WorkerError as ex:
        elapsed_ms = int((time.perf_counter() - started) * 1000)
        response = _error_result(operation, expression, ex.code, ex.message, elapsed_ms)
    except json.JSONDecodeError:
        elapsed_ms = int((time.perf_counter() - started) * 1000)
        response = _error_result(operation, expression, "parse_error", "Invalid JSON input.", elapsed_ms)
    except Exception:
        elapsed_ms = int((time.perf_counter() - started) * 1000)
        response = _error_result(operation, expression, "computation_failed", "Symbolic computation failed.", elapsed_ms)

    sys.stdout.write(json.dumps(response, ensure_ascii=False))


if __name__ == "__main__":
    main()
