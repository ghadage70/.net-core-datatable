﻿
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;

namespace DataTables.AspNet.AspNetCore
{
    /// <summary>
    /// For internal use only.
    /// Represents a DataTables request.
    /// </summary>
    internal class DataTablesRequest : Core.IDataTablesRequest
    {
        public IDictionary<string, object> AdditionalParameters { get; private set; }
        public IEnumerable<Core.IColumn> Columns { get; private set; }
        public int Draw { get; private set; }
        public int Length { get; private set; }
        public Core.ISearch Search { get; private set; }
        public int Start { get; private set; }

        public DataTablesRequest(int draw, int start, int length, Core.ISearch search, IEnumerable<Core.IColumn> columns)
            :this(draw, start, length, search, columns, null)
        { }
        public DataTablesRequest(int draw, int start, int length, Core.ISearch search, IEnumerable<Core.IColumn> columns, IDictionary<string, object> additionalParameters)
        {
            Draw = draw;
            Start = start;
            Length = length;
            Search = search;
            Columns = columns;
            AdditionalParameters = additionalParameters;
        }
    }
    public class DataTablesParameterParser
    {

        // Parses custom parameters sent with the request.
        public IDictionary<string, object> DataTablesParser(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var values = new Dictionary<string, object>();

            var sampleParameter = bindingContext.ValueProvider.GetValue("p1");
            values.Add("p1", sampleParameter.FirstValue);

            return values;
        }

    }
}
