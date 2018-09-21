
using DataTables.AspNet;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static DataTables.AspNet.AspNetCore.DataTableWrapper;

namespace DataTables.AspNet.AspNetCore
{
    /// <summary>
    /// Provides extension methods for DataTables response creation.
    /// </summary>
    public static class DataTablesExtensions
    {
        public static Core.IDataTablesResponse CreateResponse(this Core.IDataTablesRequest request, string errorMessage)
        {
            return request.CreateResponse(errorMessage, null);
        }
        public static Core.IDataTablesResponse CreateResponse(this Core.IDataTablesRequest request, string errorMessage, IDictionary<string, object> additionalParameters)
        {
            return DataTablesResponse.Create(request, errorMessage, additionalParameters);
        }
        public static Core.IDataTablesResponse CreateResponse(this Core.IDataTablesRequest request, int totalRecords, int totalRecordsFiltered, object data)
        {
            return request.CreateResponse(totalRecords, totalRecordsFiltered, data, null);
        }
        public static Core.IDataTablesResponse CreateResponse(this Core.IDataTablesRequest request, int totalRecords, int totalRecordsFiltered, object data, IDictionary<string, object> additionalParameters)
        {
            return DataTablesResponse.Create(request, totalRecords, totalRecordsFiltered, data, additionalParameters);
        }
        public static DataTablesResponse ToDataSourceResult<TModel, TResult>(this IQueryable<TModel> queryable, Core.IDataTablesRequest request, Func<TModel, TResult> selector)
        {
            if (request == null)
            {
                return null;
            }

            if (Configuration.Options.IsDrawValidationEnabled)
            {
                if (request.Draw < 1)
                {
                    return null;
                }
            }
            return queryable.ToList().AsQueryable<TModel>().CreateDataSourceResult<TModel, TResult>(request, null, selector);
        }
        public static DataTablesResponse ToDataSourceResult<TModel, TResult>(this List<TModel> queryable, Core.IDataTablesRequest request, Func<TModel, TResult> selector)
        {
            if (request == null)
            {
                return null;
            }

            if (Configuration.Options.IsDrawValidationEnabled)
            {
                if (request.Draw < 1)
                {
                    return null;
                }
            }
            return queryable.AsQueryable<TModel>().CreateDataSourceResult<TModel, TResult>(request, null, selector);
        }
        private static Expression<Func<T, bool>> GetExpression<T>(string propertyName, string propertyValue)
        {
            var parameterExp = Expression.Parameter(typeof(T), "type");
            var propertyExp = Expression.Property(parameterExp, propertyName);
            if (propertyExp.Type == typeof(string))
            {
                MethodInfo method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                var someValue = Expression.Constant(propertyValue, typeof(string));
                var containsMethodExp = Expression.Call(propertyExp, method, someValue);

                return Expression.Lambda<Func<T, bool>>(containsMethodExp, parameterExp);
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(propertyExp.Type);
                var result = converter.ConvertFrom(propertyValue);
                var someValue = Expression.Constant(result);
                var containsMethodExp = Expression.Equal(propertyExp, someValue);
                return Expression.Lambda<Func<T, bool>>(containsMethodExp, parameterExp);
            }
        }
        private static List<Expression<Func<T, bool>>> GetExpression<T>(IEnumerable<string> propertyName, string propertyValue)
        {
            List<Expression<Func<T, bool>>> lambda = new List<Expression<Func<T, bool>>>();
            var parameterExp = Expression.Parameter(typeof(T), "type");
            foreach (var item in propertyName)
            {
                var propertyExp = Expression.Property(parameterExp, item);
                if (propertyExp.Type == typeof(string))
                {
                    MethodInfo method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    var someValue = Expression.Constant(propertyValue, typeof(string));
                    var containsMethodExp = Expression.Call(propertyExp, method, someValue);
                    lambda.Add(Expression.Lambda<Func<T, bool>>(containsMethodExp, parameterExp));
                }
                else
                {
                    if (propertyValue.All(char.IsNumber))
                    {
                        var converter = TypeDescriptor.GetConverter(propertyExp.Type);
                        var result = converter.ConvertFrom(propertyValue);
                        var someValue = Expression.Constant(result);
                        var containsMethodExp = Expression.Equal(propertyExp, someValue);
                        lambda.Add(Expression.Lambda<Func<T, bool>>(containsMethodExp, parameterExp));
                    }
                }
            }
            return lambda;
        }
        private static IOrderedQueryable<T> ApplyOrder<T>
            (this IQueryable<T> queryable, string propertyName, string sortMethodName)
        {
            //build an expression tree that can be passed as lambda to IQueryable#OrderBy
            var type = typeof(T);
            var paramExpression = Expression.Parameter(type, "parameterExpression");

            var property = type.GetProperty(propertyName);
            var propertyExpression = Expression.Property(paramExpression, property);

            var lambdaType = typeof(Func<,>).MakeGenericType(type, property.PropertyType);
            var lambdaExpression = Expression.Lambda(lambdaType, propertyExpression, paramExpression);

            // dynamically generate a method with the correct type parameters
            var queryableType = typeof(Queryable);
            var orderByMethod = queryableType.GetMethods()
                .Single(m => m.Name == sortMethodName &&
                             m.IsGenericMethodDefinition
                             && m.GetGenericArguments().Length == 2
                             && m.GetParameters().Length == 2)
                .MakeGenericMethod(type, property.PropertyType);

            var result = orderByMethod.Invoke(null, new object[] { queryable, lambdaExpression });
            return (IOrderedQueryable<T>)result;
        }
        private static DataTablesResponse CreateDataSourceResult<TModel, TResult>(this IQueryable<TModel> queryable, Core.IDataTablesRequest request, ModelStateDictionary modelState, Func<TModel, TResult> selector)
        {
            if (queryable?.Any() != true || request.Length == 0)
            {
                return default;
            }
            ParameterExpression argParam = Expression.Parameter(typeof(TResult));
            IEnumerable<string> SerchableProperties = request?.Columns?.Where(e => e.IsSearchable == true)?.Select(e => e.Field);
            IEnumerable<string> ModelProperties = typeof(TResult).GetProperties().Select(e => e.Name).AsEnumerable();
            var ValidProperties = SerchableProperties.Where(e => ModelProperties.Contains(e));
            IQueryable<TModel> filteredData = queryable;
            IQueryable<TResult> data4 =(IQueryable<TResult>)filteredData.Execute<TModel, TResult>(selector).AsQueryable();
            if (!String.IsNullOrWhiteSpace(request.Search.Value))
            {
                int i = 1;
                var predicate = GetExpression<TResult>(ValidProperties, request.Search.Value);
                Expression<Func<TResult, bool>> expression = null;
                Expression ZorQ = null;
                foreach (var item in predicate)
                {
                    if (i == 1)
                    {
                        expression = item;
                        i++;
                        continue;
                    }
                    else
                    {
                        ZorQ = ZorQ != null ? ZorQ = Expression.Or(ZorQ, item.Body) : Expression.Or(expression.Body, item.Body);
                        i++;
                    }
                }
                if (expression != null && i == 2)
                {
                    ZorQ = expression.Body;
                    ZorQ = new ParameterReplacer(argParam).Visit(ZorQ);
                }
                else
                {
                    ZorQ = (BinaryExpression)new ParameterReplacer(argParam).Visit(ZorQ);
                }
                if (ZorQ != null)
                {
                    Expression<Func<TResult, bool>> predicate1 = Expression.Lambda<Func<TResult, bool>>(ZorQ, argParam);
                    data4 = data4.Where(predicate1);
                }
            }
            string Field = request.Columns?.Where(e => e.Sort != null)?.ToList()?.FirstOrDefault()?.Field;
            if (ValidProperties.Contains(Field))
            {
                string Direction = request.Columns?.Where(e => e.Sort != null)?.ToList()?.FirstOrDefault()?.Sort?.Direction.ToString();
                data4 = data4.ApplyOrder(Field, Direction == "Ascending" ? "OrderBy" : "OrderByDescending");
            }

            int TotalRecords = queryable.Count();
            int TotalRecordsFiltered = 0;
            dynamic data = null;
            if (selector == null)
            {
                TotalRecordsFiltered = ((List<TModel>)data4).Count;
                data =((List<TModel>)data4).Skip(request.Start).Take(request.Length);
            }
            else
            {
                TotalRecordsFiltered = data4.ToList().Count;
                data = data4.ToList().Skip(request.Start).Take(request.Length);
            }
            DataTablesResponse dataTablesResponse = new DataTablesResponse
            {
                Draw = request.Draw,
                TotalRecords = TotalRecords,
                TotalRecordsFiltered = TotalRecordsFiltered,
                Data = data
            };
           
            return  dataTablesResponse;
        }
        private static IEnumerable Execute<TModel, TResult>(this IQueryable<TModel> source, Func<TModel, TResult> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is DataTableWrapper)
            {
                return (IEnumerable)source;
            }

            Type elementType = source.ElementType;
            if (selector != null)
            {
                List<TResult> resultList = new List<TResult>();
                IEnumerator enumerator1 = source.GetEnumerator();
                try
                {
                    while (enumerator1.MoveNext())
                    {
                        TModel current = (TModel)enumerator1.Current;
                        resultList.Add(selector(current));
                    }
                }
                finally
                {
                    if (enumerator1 is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                return (IEnumerable)resultList;
            }
            IList instance = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            IEnumerator enumerator2 = source.GetEnumerator();
            try
            {
                while (enumerator2.MoveNext())
                {
                    object current = enumerator2.Current;
                    instance.Add(current);
                }
            }
            finally
            {
                if (enumerator2 is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            return (IEnumerable)instance;
        }

        public static IEnumerable<T> MultipleWhere<T>(this IEnumerable<T> source, Expression predicate)
        {
            foreach (T element in source)
            {
                //if (predicate(element))
                //{
                    yield return element;
                //}
            }
        }
    }
    internal class DataTableWrapper : IEnumerable, IEnumerable<DataRowView>
    {
        internal DataTableWrapper(DataTable dataTable)
        {
            Table = dataTable;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public DataTable Table { get; private set; }

        [DebuggerHidden]
        public IEnumerator<DataRowView> GetEnumerator()
        {
            // ISSUE: object of a compiler-generated type is created
            return default;
          //  return (IEnumerator<DataRowView>)new DataTableWrapper.<GetEnumerator >()
          //{
          //      this = this
          //};
        }
        internal class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter;

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return base.VisitParameter(_parameter);
            }

            internal ParameterReplacer(ParameterExpression parameter)
            {
                _parameter = parameter;
            }
        }
    }
}
