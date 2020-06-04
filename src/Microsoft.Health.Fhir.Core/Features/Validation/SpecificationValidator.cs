﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
using Microsoft.Health.Fhir.Core.Models;
using Task = System.Threading.Tasks.Task;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Microsoft.Health.Fhir.Core.Features.Validation
{
    public class SpecificationValidator : IPropertyValidator
    {
        private readonly IModelSpecificationValidator _modelValidator;

        public SpecificationValidator(IModelSpecificationValidator modelValidator)
        {
            EnsureArg.IsNotNull(modelValidator, nameof(modelValidator));

            _modelValidator = modelValidator;
        }

        public PropertyValidatorOptions Options { get; set; } = new PropertyValidatorOptions();

        public IEnumerable<ValidationFailure> Validate(PropertyValidatorContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            if (context.PropertyValue is ResourceElement resourceElement)
            {
                return _modelValidator.Validate(resourceElement);
            }

            return Enumerable.Empty<ValidationFailure>();
        }

        public Task<IEnumerable<ValidationFailure>> ValidateAsync(PropertyValidatorContext context, CancellationToken cancellation)
        {
            return Task.FromResult(Validate(context));
        }

        public bool ShouldValidateAsync(ValidationContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            return context.InstanceToValidate is ResourceElement;
        }
    }
}