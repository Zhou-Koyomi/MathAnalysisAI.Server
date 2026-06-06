# Symbolic Worker Prototype

`symbolic_worker.py` 是一个基于 SymPy 的 stdin/stdout JSON worker 原型。

## 用法

```bash
echo '{"operation":"limit","expression":"sin(x)/x","variable":"x","point":"0"}' | python3 Tools/Symbolic/symbolic_worker.py
```

## 支持 operation

- simplify
- expand
- factor
- diff
- integrate
- limit
- solve
- series

## 注意

- 仅支持 `plain/sympy` 输入字符串。
- `latex` 输入当前阶段不支持。
- stdout 仅输出 JSON。
- 不使用 eval。
