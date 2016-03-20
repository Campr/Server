using System;
using System.Linq;
using RethinkDb.Driver.Ast;

namespace Campr.Server.Lib.Extensions
{
    public static class ReqlExtensions
    {
        public static ReqlExpr BetterOr(this ReqlExpr r, params object[] exprs)
        {
            switch (exprs.Length)
            {
                // Make sure we have at least one expression.
                case 0:
                    throw new ArgumentException("At least one expression is required.", nameof(exprs));
                // If we only have one expression, no need to do anything.
                case 1:
                    return r;
                // Otherwise, combine using the builtin Or.
                default:
                    return r.Or(exprs[0], exprs.Skip(1).ToArray());
            }
        }
    }
}