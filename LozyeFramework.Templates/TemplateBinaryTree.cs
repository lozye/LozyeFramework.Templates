using System;
using System.Linq;

namespace LozyeFramework.Common.Templates
{
    /// <summary>
    /// 语义树
    /// </summary>
    internal class TemplateBinaryTree
    {
        /// <summary>
        /// 父节点
        /// </summary>
        public TemplateBinaryTree Parent;
        /// <summary>
        /// 左节点 
        /// </summary>
        public TemplateBinaryTree Left;
        /// <summary>
        /// 右节点 
        /// </summary>
        public TemplateBinaryTree Right;
        /// <summary>
        /// 操作运算符
        /// </summary>
        public TemplateBinaryOpr BinOpr;
        /// <summary>
        /// 值
        /// </summary>
        public string Value;
        /// <summary>
        /// 函数参数 <see cref="BinArgs"/> == <see cref="TemplateBinaryOpr.OPR_METHOD"/> 时生效
        /// </summary>
        public TemplateBinaryTree[] BinArgs;

        private string ToLex(bool append)
        {
            if (BinOpr == TemplateBinaryOpr.OPR_NOBINOPR) return string.Format("{0}{1}", Left == null ? "" : $"{Left.ToLex(true)}.", Value);
            if (BinOpr == TemplateBinaryOpr.OPR_METHOD) return string.Format("{0}{1}({2})", Left == null ? "" : $"{Left.ToLex(true)}.", Value, string.Join(", ", BinArgs.Select(x => x.ToLex(true))));
            if (BinOpr == TemplateBinaryOpr.OPR_INDEX) return string.Format("{0}[{1}]", Left.ToLex(true), string.Join(", ", BinArgs.Select(x => x.ToLex(true))));
            if (BinOpr == TemplateBinaryOpr.OPR_ORNOT) return $"(!{Right.ToLex(true)})";
            var symbol = TemplateBinary.Instance.Lex(BinOpr);
            if (Left != null && Right != null)
            {
                if (append) return $"({Left.ToLex(true)} {symbol} {Right.ToLex(true)})";
                return $"{Left.ToLex(true)} {symbol} {Right.ToLex(true)}";
            }
            else throw new Exception("'BinaryTree' syntax exception.");
        }
        public override string ToString() => ToLex(false);
    }
}
