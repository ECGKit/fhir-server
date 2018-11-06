﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Health.Fhir.Core.Features.Validation;
using Microsoft.Health.Fhir.Core.Features.Validation.Narratives;
using Microsoft.Health.Fhir.Core.Messages.Create;

namespace Microsoft.Health.Fhir.Core.Features.Resources.Create
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Follows validator naming convention.")]
    public class CreateResourceValidator : AbstractValidator<CreateResourceRequest>
    {
        public CreateResourceValidator(INarrativeHtmlSanitizer htmlSanitizer)
        {
            RuleFor(x => x.Resource)
                .SetValidator(new ResourceValidator(htmlSanitizer));
        }
    }
}
