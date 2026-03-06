// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DotLiquid;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;

namespace Microsoft.Health.Fhir.Liquid.Converter.DotLiquids
{
    public class MemoryFileSystem : IFhirConverterTemplateFileSystem
    {
        private readonly List<Dictionary<string, Template>> _templateCollection;

        public MemoryFileSystem(List<Dictionary<string, Template>> templateCollection)
        {
            _templateCollection = new List<Dictionary<string, Template>>();
            foreach (var templates in templateCollection)
            {
                _templateCollection.Add(new Dictionary<string, Template>(templates, templates.Comparer));
            }
        }

        public string ReadTemplateFile(Context context, string templateName)
        {
            throw new NotImplementedException();
        }

        public Template GetTemplate(Context context, string templateName)
        {
            var normalizedTemplateName = NormalizeTemplateName(templateName);
            var templatePath = GetTemplatePath(context, normalizedTemplateName);
            if (!string.IsNullOrEmpty(templatePath))
            {
                var template = GetTemplate(templatePath);
                if (template != null)
                {
                    return template;
                }
            }

            return GetTemplate(normalizedTemplateName) ?? throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, normalizedTemplateName));
        }

        public Template GetTemplate(string templateName, string rootTemplateParentPath = "")
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return null;
            }

            foreach (var templatePath in GetTemplateCandidates(templateName, rootTemplateParentPath))
            {
                foreach (var templates in _templateCollection)
                {
                    if (templates != null && templates.TryGetValue(templatePath, out var template))
                    {
                        return template;
                    }
                }
            }

            return null;
        }

        private string GetTemplatePath(Context context, string templateName)
        {
            // Get root template's parent path. This to account for cases where the root template is in a subfolder.
            var rootTemplateParentPath = context[TemplateUtility.RootTemplateParentPathScope]?.ToString();

            var templatePath = HasExplicitPath(templateName)
                ? templateName
                : NormalizeTemplateName(context[templateName]?.ToString() ?? templateName);

            return TemplateUtility.GetFormattedTemplatePath(templatePath, rootTemplateParentPath);
        }

        private static IEnumerable<string> GetTemplateCandidates(string templateName, string rootTemplateParentPath)
        {
            var normalizedTemplateName = NormalizeTemplateName(templateName);
            if (string.IsNullOrEmpty(normalizedTemplateName))
            {
                yield break;
            }

            var formattedTemplateName = TemplateUtility.GetFormattedTemplatePath(normalizedTemplateName, rootTemplateParentPath);
            yield return formattedTemplateName;

            var alternatePartialTemplateName = GetAlternatePartialTemplateName(normalizedTemplateName);
            if (!string.Equals(alternatePartialTemplateName, normalizedTemplateName, StringComparison.Ordinal))
            {
                yield return TemplateUtility.GetFormattedTemplatePath(alternatePartialTemplateName, rootTemplateParentPath);
            }
        }

        private static string GetAlternatePartialTemplateName(string templateName)
        {
            if (TemplateUtility.IsJsonSchemaTemplate(templateName))
            {
                return templateName;
            }

            var normalizedTemplateName = NormalizeTemplateName(templateName);
            var lastSeparatorIndex = normalizedTemplateName.LastIndexOf('/');
            if (lastSeparatorIndex < 0 || lastSeparatorIndex == normalizedTemplateName.Length - 1)
            {
                return normalizedTemplateName;
            }

            var prefix = normalizedTemplateName.Substring(0, lastSeparatorIndex + 1);
            var leafName = normalizedTemplateName[(lastSeparatorIndex + 1)..];
            if (string.IsNullOrEmpty(leafName))
            {
                return normalizedTemplateName;
            }

            return leafName[0] == '_'
                ? prefix + leafName.Substring(1)
                : prefix + "_" + leafName;
        }

        private static bool HasExplicitPath(string templateName)
        {
            return !string.IsNullOrEmpty(templateName) &&
                   (templateName.Contains('/') || templateName.Contains('\\'));
        }

        private static string NormalizeTemplateName(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                return templateName;
            }

            return templateName.Trim().Trim('\'', '"').Replace('\\', '/');
        }
    }
}
