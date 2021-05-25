﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Core.Features.Search.Converters;
using Xunit;
using static Microsoft.Health.Fhir.Tests.Common.Search.SearchValueValidationHelper;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.UnitTests.Features.Search.Converters
{
    public class IdToStringSearchValueConverterTests : FhirTypedElementToSearchValueConverterTests<IdToStringSearchValueConverter, Id>
    {
        [Fact]
        public async Task GivenAFhirIdWithNoValue_WhenConverted_ThenNoSearchValueShouldBeCreated()
        {
            await Test(s => s.Value = null);
        }

        [Fact]
        public async Task GivenAFhirIdWithValue_WhenConverted_ThenAStringSearchValueShouldBeCreated()
        {
            const string value = "test";

            await Test(
                s => s.Value = value,
                ValidateString,
                value);
        }
    }
}
