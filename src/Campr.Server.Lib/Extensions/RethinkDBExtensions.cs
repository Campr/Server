﻿using System;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;

namespace Campr.Server.Lib.Extensions
{
    public static class RethinkDbExtensions
    {
        public static ReqlExpr BetterOr(this RethinkDB r, params object[] exprs)
        {
            switch (exprs.Length)
            {
                // Make sure we have at least one expression.
                case 0:
                    throw new ArgumentException("At least one expression is required.", nameof(exprs));
                // If we only have one expression, no need to do anything.
                case 1:
                    return (ReqlExpr)exprs[0];
                // Otherwise, combine using the builtin Or.
                default:
                    return r.Or(exprs);
            }
        }

        public static ReqlExpr BetterAnd(this RethinkDB r, params object[] exprs)
        {
            switch (exprs.Length)
            {
                // Make sure we have at least one expression.
                case 0:
                    throw new ArgumentException("At least one expression is required.", nameof(exprs));
                // If we only have one expression, no need to do anything.
                case 1:
                    return (ReqlExpr)exprs[0];
                // Otherwise, combine using the builtin And.
                default:
                    return r.And(exprs);
            }
        }
    }
}