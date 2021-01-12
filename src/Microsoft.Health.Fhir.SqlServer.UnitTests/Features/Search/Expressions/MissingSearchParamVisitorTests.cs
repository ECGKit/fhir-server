﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Fhir.Core.Features.Search.Expressions;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.SqlServer.Features.Search.Expressions;
using Microsoft.Health.Fhir.SqlServer.Features.Search.Expressions.Visitors;
using Xunit;

namespace Microsoft.Health.Fhir.SqlServer.UnitTests.Features.Search.Expressions
{
    public class MissingSearchParamVisitorTests
    {
        [Fact]
        public void GivenExpressionWithMissingParameterExpression_WhenVisited_AllExpressionPrependedToExpressionList()
        {
            var tableExpressions = new List<SearchParamTableExpression>
            {
                new SearchParamTableExpression(null, new MissingSearchParameterExpression(new SearchParameterInfo("TestParam"), true), SearchParamTableExpressionKind.Normal),
            };

            var inputExpression = SqlRootExpression.WithSearchParamTableExpressions(tableExpressions);
            var visitedExpression = (SqlRootExpression)inputExpression.AcceptVisitor(MissingSearchParamVisitor.Instance);
            Assert.Collection(
                visitedExpression.SearchParamTableExpressions,
                e => { Assert.Equal(SearchParamTableExpressionKind.All, e.Kind); },
                e => { Assert.NotNull(e.Predicate as MissingSearchParameterExpression); });
            Assert.Equal(tableExpressions.Count + 1, visitedExpression.SearchParamTableExpressions.Count);
        }

        [Fact]
        public void GivenExpressionWithNoMissingParameterExpression_WhenVisited_OriginalExpressionReturned()
        {
            var tableExpressions = new List<SearchParamTableExpression>
            {
                new SearchParamTableExpression(null, null, SearchParamTableExpressionKind.Normal),
            };

            var inputExpression = SqlRootExpression.WithSearchParamTableExpressions(tableExpressions);
            var visitedExpression = (SqlRootExpression)inputExpression.AcceptVisitor(MissingSearchParamVisitor.Instance);
            Assert.Equal(inputExpression, visitedExpression);
        }

        [Fact]
        public void GivenExpressionWithMissingParameterExpressionFalseLast_WhenVisited_OriginalExpressionReturned()
        {
            var tableExpressions = new List<SearchParamTableExpression>
            {
                new SearchParamTableExpression(null, null, SearchParamTableExpressionKind.Normal),
                new SearchParamTableExpression(null, new MissingSearchParameterExpression(new SearchParameterInfo("TestParam"), false), SearchParamTableExpressionKind.Normal),
            };

            var inputExpression = SqlRootExpression.WithSearchParamTableExpressions(tableExpressions);
            var visitedExpression = (SqlRootExpression)inputExpression.AcceptVisitor(MissingSearchParamVisitor.Instance);
            Assert.Equal(inputExpression, visitedExpression);
        }

        [Fact]
        public void GivenExpressionWithMissingParameterExpressionLast_WhenVisited_MissingParameterExpressionNegated()
        {
            var tableExpressions = new List<SearchParamTableExpression>
            {
                new SearchParamTableExpression(null, null, SearchParamTableExpressionKind.Normal),
                new SearchParamTableExpression(null, new MissingSearchParameterExpression(new SearchParameterInfo("TestParam"), true), SearchParamTableExpressionKind.Normal),
            };

            var inputExpression = SqlRootExpression.WithSearchParamTableExpressions(tableExpressions);
            var visitedExpression = (SqlRootExpression)inputExpression.AcceptVisitor(MissingSearchParamVisitor.Instance);
            Assert.Collection(
                visitedExpression.SearchParamTableExpressions,
                e => { Assert.Equal(tableExpressions[0], e); },
                e => { Assert.Equal(SearchParamTableExpressionKind.NotExists, e.Kind); });
        }
    }
}
