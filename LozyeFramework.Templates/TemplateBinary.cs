#if DEBUG2
using LozyeFramework.Common.Shouldly;
#endif
using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace LozyeFramework.Common.Templates
{
    /// <summary>
    /// 操作运算符
    /// </summary>
    internal enum TemplateBinaryOpr : int
    {
        OPR_ADD, OPR_SUB, OPR_MUL, OPR_DIV, OPR_MOD,    /* ORDER ARITH */
        OPR_POW, OPR_CONCAT,
        OPR_EQ, OPR_NE,
        OPR_LT, OPR_GE, OPR_LE, OPR_GT,
        OPR_ANDALSO, OPR_ORALSO,
        OPR_NULLOR, OPR_ORNOT,                          /* OPR_ORNOT SEEAS OPR_NOBINOPR USE RIGHT */
        OPR_BLOCK, OPR_BLOCKEND,
        OPR_NOBINOPR, OPR_METHOD,
        OPR_INDEX
    }

    internal class TemplateBinary
    {
        private TemplateBinary() { }
        private static readonly Lazy<TemplateBinary> lazyInstance = new Lazy<TemplateBinary>(() => new TemplateBinary());
        public static TemplateBinary Instance => lazyInstance.Value;

        private static readonly int[] priority_set = new[] {
            6, 6, 7, 7, 7,	                    /* ADD SUB MUL DIV MOD */
            10, 5,			                    /* POW CONCAT */
            3, 3,				                /* EQ NE */
            3, 3, 3, 3,		                    /* LT GE LE GT */
            2, 1,				                /* ANDALSO ORALSO */            
            11, 11,                             /* NULLOR ORNOT */
            12, 12,	                            /* BLOCK BLOCKEND */
        };
        private static readonly string[] priority_symbol = new[] {
            "+", "-", "*", "/", "%",	        /* ADD SUB MUL DIV MOD */
            "^", "..",		                    /* POW CONCAT */
            "==", "!=",				            /* EQ NE */
            "<", ">=", "<=", ">",		        /* LT GE LE GT */
            "&&", "||",				            /* ANDALSO ORALSO */            
            "??", "!",                          /* NULLOR ORNOT */
            "(", ")",	                        /* BLOCK BLOCKEND */
            ".val", ".call",                    /* NOBINOPR METHOD */
            ".index",                           /* INDEX */
        };

        const string EXCEPTION_BINARY = "expression syntax error.";

        /// <summary>
        /// 获取操作符优先级
        /// </summary>
        /// <param name="opr"></param>
        /// <returns></returns>
        public int Priority(TemplateBinaryOpr opr) => priority_set[(int)opr];

        /// <summary>
        /// 获取操作符操作符
        /// </summary>
        /// <param name="opr"></param>
        /// <returns></returns>
        public string Lex(TemplateBinaryOpr opr) => priority_symbol[(int)opr];

        /// <summary>
        /// 语义二叉树
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public TemplateBinaryTree Interpret(ReadOnlySpan<char> tokens)
        {
            if (tokens.Length == 0) throw new FormatException(EXCEPTION_BINARY);
            TemplateBinaryTree root = null, curr = null;
            TemplateBinaryTree tmp;
            int i = 0, prev = i;
            TemplateBinaryOpr opr;

            while (i < tokens.Length)
            {
                // 检索下一个操作符
                opr = MoveNext(tokens, ref i, ref prev, out int len);
                // '!' 操作符特殊处理
                if (opr == TemplateBinaryOpr.OPR_ORNOT)
                {
                    prev = ++i;
                    // '!' 为一元操作符，故检索下一个操作符
                    opr = MoveNext(tokens, ref i, ref prev, out len);
                    // '!' 操作符 为一元操作符 仅使用Right节点
                    tmp = new TemplateBinaryTree { BinOpr = TemplateBinaryOpr.OPR_ORNOT, Right = Value(tokens, prev, i - len) };
                }
                // '(' 代码块、方法特殊处理
                else if (opr == TemplateBinaryOpr.OPR_BLOCK || opr == TemplateBinaryOpr.OPR_INDEX)
                {
                    // 索引处理
                    if (opr == TemplateBinaryOpr.OPR_INDEX) tmp = Index(tokens, prev, ref i);
                    // '(' 为起始操作符则视为代码块
                    else if (prev == i) tmp = Block(tokens, ref i);
                    // '(' 视为方法
                    else tmp = Method(tokens, prev, ref i);
                    NEXT_CHECK:;
                    // Block、Method 都视为值， 故检索下一个操作符, 检索指针前移一位 ')' 用以指代当前块的值
                    i--;
                    opr = MoveNext(tokens, ref i, ref prev, out len);
                    if (opr == TemplateBinaryOpr.OPR_BLOCK)
                    {
                        var next = MethodThen(tokens, prev + 1, ref i);
                        if (next.Left == null) next.Left = tmp;
                        else next.Left.Left = tmp;
                        tmp = next;
                        goto NEXT_CHECK;
                    }
                    else if (opr == TemplateBinaryOpr.OPR_INDEX)
                    {
                        var next = IndexThen(tokens, prev + 1, ref i);
                        if (next.Left == null) next.Left = tmp;
                        else next.Left.Left = tmp;
                        tmp = next;
                        goto NEXT_CHECK;
                    }
                    else if (opr == TemplateBinaryOpr.OPR_NOBINOPR && prev + 1 < i)
                    {
                        var next = FieldThen(tokens, prev + 1, ref i);
                        if (next != null) { next.Left = tmp; tmp = next; }
                    }
                }
                // 获取操作符之前的值
                else
                {
                    tmp = Value(tokens, prev, i - len);
                }
                // 根节点为空情况
                if (root == null)
                {
                    // 如果没有下个操作符 则直接返回值
                    if (opr == TemplateBinaryOpr.OPR_NOBINOPR) return tmp;
                    root = curr = new TemplateBinaryTree { Left = tmp, BinOpr = opr };
                }
                else
                {
                    // 运算优先级处理
                    curr = Shift(curr, opr, tmp);
                    // 如果父级为空了则视为下个运算符为根运算
                    if (curr.Parent == null) root = curr;
                }
                prev = ++i;
            }
            return root;
        }

        /// <summary>
        /// 运算优先级处理 移动过程仅需要考虑不带括号的移动
        /// </summary>
        /// <param name="left"></param>
        /// <param name="opr"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private TemplateBinaryTree Shift(TemplateBinaryTree left, TemplateBinaryOpr opr, TemplateBinaryTree t)
        {
            // 后续没有操作符的时候直接绑定到右节点上
            if (opr == TemplateBinaryOpr.OPR_NOBINOPR)
            {
                left.Right = t;
                return left;
            }
            var p = Priority(opr);
            // 下一个操作符优先级更高时 下移
            if (p > Priority(left.BinOpr))
            {
                //down priority>old
                left.Right = new TemplateBinaryTree { Left = t, BinOpr = opr, Parent = left };
                return left.Right;
            }
            // 下一个操作符优先级一致或更低时 上移
            else
            {
                // up               
                left.Right = t;

                // 上移操作符 直到父级操作符优先级更低
                var cursor = left;
                while (cursor.Parent != null && p <= Priority(cursor.Parent.BinOpr)) { cursor = cursor.Parent; }

                var tmp = new TemplateBinaryTree { Left = cursor, BinOpr = opr };
                // 父级不为空时 交换节点
                if (cursor.Parent != null)
                {
                    cursor.Parent.Right = tmp;
                    tmp.Parent = cursor.Parent;
                }
                cursor.Parent = tmp;
                return tmp;
            }
        }

        /// <summary>
        /// 获取值表达式， 解释结果视为值
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="prev"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private TemplateBinaryTree Value(ReadOnlySpan<char> tokens, int prev, int i)
            => new TemplateBinaryTree { Value = Val(tokens, prev, i).ToString(), BinOpr = TemplateBinaryOpr.OPR_NOBINOPR };

        /// <summary>
        /// 值解释
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="prev"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private ReadOnlySpan<char> Val(ReadOnlySpan<char> tokens, int prev, int i)
        {
#if DEBUG2
            XAssert.When(prev >= i);
#endif
            if (prev == i) throw new FormatException(EXCEPTION_BINARY);
            return tokens.Slice(prev, i - prev).Trim();
        }

        /// <summary>
        /// 方法块解释， 解释结果视为值
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="prev"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private TemplateBinaryTree Method(ReadOnlySpan<char> tokens, int prev, ref int i)
        {
            var val = Val(tokens, prev, i);
            var idx = val.LastIndexOf('.');
            var left = idx > 0 ? Interpret(val.Slice(0, idx)) : null;
            var method = idx > 0 ? val.Slice(idx + 1).Trim() : val.Trim();
            var block = MoveBlock(tokens, ')', ref i);
            return new TemplateBinaryTree
            {
                BinOpr = TemplateBinaryOpr.OPR_METHOD,
                Left = left,
                Value = method.ToString(),
                BinArgs = Arguments(block).ToArray()
            };
        }

        /// <summary>
        /// 连续调用Method
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="prev"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private TemplateBinaryTree MethodThen(ReadOnlySpan<char> tokens, int prev, ref int i)
        {
            var val = Val(tokens, prev, i);
            var idx = val.LastIndexOf('.');
            if (idx == -1) throw new FormatException(EXCEPTION_BINARY);
            var method = val.Slice(idx + 1).Trim();
            var block = MoveBlock(tokens, ')', ref i);
            return new TemplateBinaryTree
            {
                BinOpr = TemplateBinaryOpr.OPR_METHOD,
                Value = method.ToString(),
                Left = idx == 0 ? null : Field(val.Slice(0, idx)),
                BinArgs = Arguments(block).ToArray()
            };
        }

        /// <summary>
        /// 方法块解释， 解释结果视为值
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="prev"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private TemplateBinaryTree Index(ReadOnlySpan<char> tokens, int prev, ref int i)
        {
            var val = Val(tokens, prev, i);
            var block = MoveBlock(tokens, ']', ref i);
            return new TemplateBinaryTree
            {
                BinOpr = TemplateBinaryOpr.OPR_INDEX,
                Left = Interpret(val),
                BinArgs = Arguments(block).ToArray(),
            };
        }

        /// <summary>
        /// 连续调用Index
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="prev"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private TemplateBinaryTree IndexThen(ReadOnlySpan<char> tokens, int prev, ref int i)
        {
            var val = Val(tokens, prev, i);
            var block = MoveBlock(tokens, ']', ref i);
            return new TemplateBinaryTree
            {
                BinOpr = TemplateBinaryOpr.OPR_INDEX,
                Left = val.Length > 0 ? Field(val) : null,
                BinArgs = Arguments(block).ToArray(),
            };
        }

        /// <summary>
        /// 括号后访问属性
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="prev"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private TemplateBinaryTree Field(ReadOnlySpan<char> tokens)
        {
            if (tokens[0] != '.' || tokens[tokens.Length - 1] == '.')
                throw new FormatException(EXCEPTION_BINARY);
            return new TemplateBinaryTree
            {
                BinOpr = TemplateBinaryOpr.OPR_NOBINOPR,
                Value = tokens.Slice(1).Trim().ToString()
            };
        }

        private TemplateBinaryTree FieldThen(ReadOnlySpan<char> tokens, int prev, ref int i)
        {
            var val = Val(tokens, prev, i);
            if (val.Length == 0) return null;
            return Field(val);
        }

        /// <summary>
        /// 括号块解释， 解释结果视为值
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private TemplateBinaryTree Block(ReadOnlySpan<char> tokens, ref int i)
        {
            var block = MoveBlock(tokens, ')', ref i);
            return Interpret(block);
        }

        /// <summary>
        /// 获取方法参数
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private List<TemplateBinaryTree> Arguments(ReadOnlySpan<char> tokens)
        {
            int i = 0, prev = i;
            List<TemplateBinaryTree> args = new List<TemplateBinaryTree>(8);
            if (tokens.Length == 0) return args;
            while (MoveComma(tokens, ref i))
            {
                args.Add(Interpret(tokens.Slice(prev, i - prev)));
                prev = ++i;
            }
            args.Add(Interpret(tokens.Slice(prev, i - prev)));
            return args;
        }

        /// <summary>
        /// 判断是否为数字
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private bool IsDigit(char c) => '0' <= c && c <= '9';

        /// <summary>
        /// 断言下一个操作符为参数 <paramref name="ch"/>
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="i"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        private bool IsNext(ReadOnlySpan<char> tokens, int i, char ch) => ++i < tokens.Length && tokens[i] == ch;

        /// <summary>
        /// 获取下一个操作符
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="i"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private TemplateBinaryOpr MoveNext(ReadOnlySpan<char> tokens, ref int i, ref int prev, out int len)
        {
            prev = i;
            len = 0;
            for (; i < tokens.Length; i++)
            {
                var ch = tokens[i];
                if (ch == ' ') { if (prev == i) prev = i + 1; continue; }
                switch (ch)
                {
                    case '\'':
                    case '\"': MoveText(tokens, ch, ref i); break;
                    case '[': return TemplateBinaryOpr.OPR_INDEX;
                    case '(': return TemplateBinaryOpr.OPR_BLOCK;
                    case '+': return TemplateBinaryOpr.OPR_ADD;
                    case '-':
                        {
                            if (prev == i)
                            {
                                // '-' 负号当且仅当后续为数字的时候视为负数
                                if (IsDigit(tokens[i + 1])) continue;
                                else throw new FormatException(EXCEPTION_BINARY);
                            }
                            return TemplateBinaryOpr.OPR_SUB;
                        }
                    case '*': return TemplateBinaryOpr.OPR_MUL;
                    case '/': return TemplateBinaryOpr.OPR_DIV;
                    case '%': return TemplateBinaryOpr.OPR_MOD;
                    case '^': return TemplateBinaryOpr.OPR_POW;
                    case '.':
                        {
                            // '..' 字符串连接特殊操作符
                            if (IsNext(tokens, i, '.')) { i = i + (len = 1); return TemplateBinaryOpr.OPR_CONCAT; }
                            continue;
                        }
                    case '!':
                        {
                            if (IsNext(tokens, i, '=')) { i = i + (len = 1); return TemplateBinaryOpr.OPR_NE; }
                            if (prev == i) return TemplateBinaryOpr.OPR_ORNOT;
                            throw new FormatException(EXCEPTION_BINARY);
                        }
                    case '=':
                        {
                            if (IsNext(tokens, i, '=')) { i = i + (len = 1); return TemplateBinaryOpr.OPR_EQ; }
                            // 不支持赋值运算
                            throw new FormatException(EXCEPTION_BINARY);
                        };
                    case '<':
                        {
                            if (IsNext(tokens, i, '=')) { i = i + (len = 1); return TemplateBinaryOpr.OPR_LE; }
                            return TemplateBinaryOpr.OPR_LT;
                        };
                    case '>':
                        {
                            if (IsNext(tokens, i, '=')) { i = i + (len = 1); return TemplateBinaryOpr.OPR_GE; }
                            return TemplateBinaryOpr.OPR_GT;
                        };
                    case '&':
                        {
                            if (IsNext(tokens, i, '&')) { i = i + (len = 1); return TemplateBinaryOpr.OPR_ANDALSO; }
                            // 不支持位运算
                            throw new FormatException(EXCEPTION_BINARY);
                        };
                    case '|':
                        {
                            if (IsNext(tokens, i, '|')) { i = i + (len = 1); return TemplateBinaryOpr.OPR_ORALSO; }
                            // 不支持位运算
                            throw new FormatException(EXCEPTION_BINARY);
                        };
                    case '?':
                        {
                            if (IsNext(tokens, i, '?')) { i = i + (len = 1); return TemplateBinaryOpr.OPR_NULLOR; }
                            // 不支持三元表达式
                            throw new FormatException(EXCEPTION_BINARY);
                        };
                }
            }
            return TemplateBinaryOpr.OPR_NOBINOPR;
        }

        /// <summary>
        /// 进行BLOCK 查找 并移除多余的 '(',')'
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private ReadOnlySpan<char> MoveBlock(ReadOnlySpan<char> tokens, char mv, ref int i)
        {
            int depth = 1;
            int prev = ++i;
            for (; i < tokens.Length; i++)
            {
                var ch = tokens[i];
                switch (ch)
                {
                    case '\'':
                    case '\"': MoveText(tokens, ch, ref i); break;
                    case '[':
                    case '(': depth++; break;
                    case ']':
                    case ')':
                        depth--;
                        if (ch != mv) continue;
                        if (depth == 0) { i++; return tokens.Slice(prev, i - prev - 1); }
                        else if (depth < 0) throw new FormatException(EXCEPTION_BINARY);
                        break;
                }
            }
            throw new FormatException(EXCEPTION_BINARY);
        }

        /// <summary>
        /// 逗号检测
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private bool MoveComma(ReadOnlySpan<char> tokens, ref int i)
        {
            int depth = 0;
            for (; i < tokens.Length; i++)
            {
                var ch = tokens[i];
                switch (ch)
                {
                    case '\'':
                    case '\"': MoveText(tokens, ch, ref i); break;
                    case '[':
                    case '(': depth++; break;
                    case ']':
                    case ')': depth--; break;
                    case ',': if (depth == 0) return true; break;
                }
            }
            if (depth != 0) throw new FormatException(EXCEPTION_BINARY);
            return false;
        }

        /// <summary>
        /// 文本字符串处理 '\"', '\'' 
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="ch"></param>
        /// <param name="i"></param>
        /// <exception cref="FormatException"></exception>
        private void MoveText(ReadOnlySpan<char> tokens, char ch, ref int i)
        {
            i++;
            for (; i < tokens.Length; i++)
                if (tokens[i] == ch && tokens[i - 1] != '\\') { return; }
            throw new FormatException(EXCEPTION_BINARY);
        }
    }
}

