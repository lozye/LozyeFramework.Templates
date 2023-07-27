# LozyeFramework.Templates
operators and expressions explan

# 基于二叉树的语法解释过程

支持的符号
```c#
"+", "-", "*", "/", "%",	        /* ADD SUB MUL DIV MOD */
"^", "..",		                    /* POW CONCAT */
"==", "!=",				            /* EQ NE */
"<", ">=", "<=", ">",		        /* LT GE LE GT */
"&&", "||",				            /* ANDALSO ORALSO */            
"??", "!",                          /* NULLOR ORNOT */
"(", ")",	                        /* BLOCK BLOCKEND */
".val", ".call"                     /* NOBINOPR METHOD */
```

测试结果
```md
--------------------------------------
expressin is (a+b*c)/2-a
root is -
((a + (b * c)) / 2) - a

--------------------------------------
expressin is a+b*c
root is +
a + (b * c)

--------------------------------------
expressin is a >= b
root is >=
a >= b

--------------------------------------
expressin is a != b
root is !=
a != b

--------------------------------------
expressin is a + str
root is +
a + str

--------------------------------------
expressin is a == str
root is ==
a == str

--------------------------------------
expressin is a < str
root is <
a < str

--------------------------------------
expressin is a ?? b-c
root is -
(a ?? b) - c

--------------------------------------
expressin is a..str.Length
root is ..
a .. str.Length

--------------------------------------
expressin is a/(b)-c*n
root is -
(a / b) - (c * n)

--------------------------------------
expressin is a/(b*a^2)
root is /
a / (b * (a ^ 2))

--------------------------------------
expressin is (a+b*c)/2+a/(b*a^2)-a+b*cc*(v+a)
root is +
((((a + (b * c)) / 2) + (a / (b * (a ^ 2)))) - a) + ((b * cc) * (v + a))

--------------------------------------
expressin is (a+b)/2+a/(b-a^2*5)
root is +
((a + b) / 2) + (a / (b - ((a ^ 2) * 5)))

--------------------------------------
expressin is calc('1+2')  +   -5/(b*c)
root is +
calc('1+2') + (-5 / (b * c))

--------------------------------------
expressin is 'name=>'  .. "\"a1111+   15*6(  a*6 )"
root is ..
'name=>' .. "\"a1111+   15*6(  a*6 )"

--------------------------------------
expressin is (2*a + (3*b)) / (4*n)
root is /
((2 * a) + (3 * b)) / (4 * n)

--------------------------------------
expressin is !a && b && c || !c
root is ||
(((!a) && b) && c) || (!c)

--------------------------------------
expressin is add(calc('a+ b'),c) +  5*   dev(  a, b)
root is +
add(calc('a+ b'), c) + (5 * dev(a, b))

--------------------------------------
expressin is tem()
root is .call
tem()
```