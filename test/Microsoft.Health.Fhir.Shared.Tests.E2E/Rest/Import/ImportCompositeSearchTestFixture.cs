﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Client;
using Microsoft.Health.Fhir.Tests.Common;
using Microsoft.Health.Fhir.Tests.Common.FixtureParameters;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Tests.E2E.Rest.Import
{
    public class ImportCompositeSearchTestFixture : ImportTestFixture<StartupForImportCompositeSearchTestProvider>
    {
        private static readonly string[] ObservationTestFileNames =
        {
            "ObservationWith1MinuteApgarScore",
            "ObservationWith20MinuteApgarScore",
            "ObservationWithEyeColor",
            "ObservationWithLongEyeColor",
            "ObservationWithTemperature",
            "ObservationWithTPMTDiplotype",
            "ObservationWithTPMTHaplotypeOne",
            "ObservationWithBloodPressure",
        };

        private static readonly string[] DocumentReferenceTestFiles =
        {
            "DocumentReference-example-relatesTo-code-appends",
            "DocumentReference-example-relatesTo-code-transforms-replaces-target",
            "DocumentReference-example-relatesTo-code-transforms",
        };

        public ImportCompositeSearchTestFixture(DataStore dataStore, Format format, TestFhirServerFactory testFhirServerFactory)
            : base(dataStore, format, testFhirServerFactory)
        {
        }

        public string TestSessionId { get; } = Guid.NewGuid().ToString();

        public IReadOnlyDictionary<string, Observation> Observations { get; private set; }

        public IReadOnlyDictionary<string, DocumentReference> DocumentReferences { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            await TestFhirClient.DeleteAllResources(ResourceType.Observation);
            await TestFhirClient.DeleteAllResources(ResourceType.DocumentReference);

            Observations = CreateResultDictionary<Observation>(ObservationTestFileNames);
            DocumentReferences = CreateResultDictionary<DocumentReference>(DocumentReferenceTestFiles);

            List<Resource> resources = new List<Resource>();
            resources.AddRange(Observations.Values);
            resources.AddRange(DocumentReferences.Values);

            await ImportTestHelper.ImportToServerAsync(
                TestFhirClient,
                CloudStorageAccount,
                resources.ToArray());
        }

        protected override async Task OnDisposedAsync()
        {
            await TestFhirClient.DeleteAllResources(ResourceType.Observation);
            await TestFhirClient.DeleteAllResources(ResourceType.DocumentReference);
        }

        private Dictionary<string, T> CreateResultDictionary<T>(string[] files)
            where T : Resource
        {
            var resultDictionary = new Dictionary<string, T>(files.Length);

            for (int i = 0; i < files.Length; i++)
            {
                string testFileName = files[i];

                T result = GetResourceFromFile<T>(testFileName);

                resultDictionary.Add(testFileName, result);
            }

            return resultDictionary;
        }

        private T GetResourceFromFile<T>(string testFileName)
            where T : Resource
        {
            T resource = Samples.GetJsonSample<T>(testFileName);

            switch (resource)
            {
                case Observation o:
                    o.Identifier.Add(new Identifier(null, TestSessionId));
                    o.Id = Guid.NewGuid().ToString("N");
                    break;
                case DocumentReference d:
                    d.Identifier.Add(new Identifier(null, TestSessionId));
                    d.Id = Guid.NewGuid().ToString("N");
                    break;
            }

            return resource;
        }
    }
}
