﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoQC_Calc
{
    /// <summary>
    /// Node that is based on a complex mathematical expression (string).
    /// </summary>
    internal class NodeExpression : Node
    {
        private string Expr;
        public NodeExpression(string expr)    /// 1 + 2
        {
            Expr = expr.Trim(' ').ToLower(); // just trim garbage spaces and normalize to lowercase.            
        }

        private string RemoveEnclosingElipsis(string eExpr)
        {
            // remove the enclosing elipsis, if they are enclosing the whole expression.
            // recursive to remove multiple elipsis,eg: (((2 + 3 )))
            // Do NOT remove in cases like (2+3) - (1-5)
            if (eExpr.StartsWith('(') && eExpr.EndsWith(')'))
            {
                int countElipsis = 1;
                bool remove = true;
                for (int j = 1; j < eExpr.Length - 1; j++)
                {
                    if (eExpr[j] == '(') countElipsis++;
                    if (eExpr[j] == ')') countElipsis--;
                    if (countElipsis == 0)
                    { // this happens when the elipsis close in the middle, like (2 + 4) * (3+1)
                        remove = false; // do not remove in that case.
                        break;
                    }
                }
                if (remove)
                {   // if it found enclosing elipsis, remove them and test to see if there is another level, this optional:
                    // we might decide that (( 2)) is not a valid expression, in which case recursion is not needed
                    eExpr = eExpr.Substring(1, eExpr.Length - 2);
                    eExpr = RemoveEnclosingElipsis(eExpr);
                }
            }
            return eExpr;
        }
        
        

        /// <summary>
        /// Splits the incoming expressions left to right, by Op.
        /// It returns the left/right expressions, ie: 
        /// {exprleft} Op {exprRight}
        /// eg: (2 + 3) * (5 + 2) => exprLeft = (2 + 3) , exprRight = (5 + 2)
        /// </summary>        
        private bool SplitByOp(string Expr, char Op, out string ExprLeft, out string ExprRight)
        {
            int elipsisCount = 0;
            for (int i = 0; i < Expr.Length; i++)
            {
                if (Expr[i] == '(') elipsisCount++; // counts whether we are inside a ( ) section
                if (Expr[i] == ')') elipsisCount--;
                if (elipsisCount == 0 && Expr[i] == Op && i>0) // can only split outside ( ), ie: at the top level
                    // also doesn't split if there is "-" at the very beginning (this is handled as a "negate").
                {
                    ExprLeft = Expr.Substring(0, i);
                    ExprRight = Expr.Substring(i + 1);
                    return true;
                }
            }
            ExprLeft = ExprRight = "";
            return false;
        }

        public override double Evaluate()
        {
            Expr = RemoveEnclosingElipsis(Expr);
            
            double exprValue;
            // is expression a number? => return it
            if (double.TryParse(Expr, out exprValue))
            {
                return exprValue;
            }           
            else
            {  // 1 + 2
                // try to split the expression by operator
                string exprLeft = "", exprRight = "";
                char[] ops = { '+', '-', '*', '/', '^' }; // highest precendence operators go last
                bool found = false;
                char oper = ' ';    // 1 + 2^2 
                                    // 1 op(+)  nodeexpression( 2 ^2 ) 
                                    //          >> pow(2,2) 
                foreach (var op in ops)
                {
                    found = SplitByOp(Expr, op, out exprLeft, out exprRight);
                    if (found)
                    {
                        oper = op;
                        break;
                    }
                }
                if (found)
                { // was able to split by operator => create NodeBinary with both sides and evaluate.

                    // special case to handle things like -(2+3)
                    //if (oper == '-' && exprLeft == "") exprLeft = "0";

                    var ne1 = new NodeExpression(exprLeft);
                    var ne2 = new NodeExpression(exprRight);
                    var nb = new NodeBinary(ne1, ne2, oper, Expr);
                    return nb.Evaluate();
                }
                else // it couldn't split => is it a call to a function? 
                { // -sqrt(2)
                    if (Expr[0] == '-') 
                    {
                        return new NodeNegate(new NodeExpression(Expr.Substring(1))).Evaluate();
                    }
                    else
                    {
                        // depending on how many functions we support, this could be a regex to detect anything like
                        // funct_name ( x ) 
                        if (Expr.StartsWith("sqrt"))
                        {
                            var nf = new NodeFunction("sqrt",
                                new NodeExpression(Expr.Substring(4)));
                            return nf.Evaluate();
                        }
                    }

                }
            }
            throw new Exception($"Unparseable: {Expr}");
        }
    }
}
