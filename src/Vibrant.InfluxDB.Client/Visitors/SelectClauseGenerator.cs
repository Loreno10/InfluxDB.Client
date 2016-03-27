﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vibrant.InfluxDB.Client.Linq;
using Vibrant.InfluxDB.Client.Metadata;
using Vibrant.InfluxDB.Client.Parsers;

namespace Vibrant.InfluxDB.Client.Visitors
{
   public class SelectClauseGenerator<TInfluxRow> : ProjectingClauseGenerator<TInfluxRow>
      where TInfluxRow : new()
   {
      internal string GetSelectClause( SelectClause clause )
      {
         InitialProjection = clause.Projection.InnerProjection;

         var bindings = clause.Projection.Bindings;

         for( int i = 0 ; i < bindings.Count ; i++ )
         {
            Visit( bindings[ i ].Source );

            if( i != bindings.Count - 1 )
            {
               Clause.Append( ", " );
            }
         }

         return Clause.ToString();
      }

      protected override Expression VisitBinary( BinaryExpression b )
      {
         Clause.Append( "(" );

         // TODO: Support ~=, !~

         Visit( b.Left );

         switch( b.NodeType )
         {
            case ExpressionType.Add:
               Clause.Append( " + " );
               break;
            case ExpressionType.Subtract:
               Clause.Append( " - " );
               break;
            case ExpressionType.Multiply:
               Clause.Append( " * " );
               break;
            case ExpressionType.Divide:
               Clause.Append( " / " );
               break;
            default:
               throw new NotSupportedException( $"The binary operator '{b.NodeType}' is not supported in a select clause." );
         }

         Visit( b.Right );

         Clause.Append( ")" );

         return b;
      }

      protected override Expression VisitMethodCall( MethodCallExpression node )
      {
         if( node.Method.DeclaringType == typeof( InfluxFunctions ) )
         {
            if( node.Method.Name == "Count" )
            {
               Clause.Append( "COUNT(" );
               Visit( node.Arguments[ 0 ] );
               Clause.Append( ")" );
            }

            return node;
         }
         else
         {
            throw new NotSupportedException( $"The method '{node.Method.Name}' on '{node.Method.DeclaringType.FullName}' is not supported in a select clause." );
         }
      }

      protected override Expression VisitConstant( ConstantExpression node )
      {
         if( node.Value == null )
         {
            Clause.Append( "NULL" );
         }
         else
         {
            var value = QueryEscape.EscapeFieldOrTag( node.Value );
            Clause.Append( value );
         }
         return node;
      }
   }
}