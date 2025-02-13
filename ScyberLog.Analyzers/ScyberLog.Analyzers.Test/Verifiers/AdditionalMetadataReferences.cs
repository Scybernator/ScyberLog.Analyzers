// Original Source: https://github.com/dotnet/roslyn-analyzers/blob/main/src/Test.Utilities/AdditionalMetadataReferences.cs
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Test.Utilities
{
    public static class AdditionalMetadataReferences
    {
        public static ReferenceAssemblies Default { get; } = CreateDefaultReferenceAssemblies();

        public static ReferenceAssemblies DefaultWithMELogging { get; } = Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.Extensions.Logging", "9.0.2")));

        private static ReferenceAssemblies CreateDefaultReferenceAssemblies()
        {
            var referenceAssemblies = ReferenceAssemblies.Default;

#if !NETCOREAPP
            referenceAssemblies = referenceAssemblies.AddAssemblies(ImmutableArray.Create("System.Xml.Data"));
#endif

            referenceAssemblies = referenceAssemblies.AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "3.0.0")));

#if NETCOREAPP
            referenceAssemblies = referenceAssemblies.AddPackages(ImmutableArray.Create(
                new PackageIdentity("System.Runtime.Serialization.Formatters", "4.3.0"),
                new PackageIdentity("System.Configuration.ConfigurationManager", "4.7.0"),
                new PackageIdentity("System.Security.Cryptography.Cng", "4.7.0"),
                new PackageIdentity("System.Security.Permissions", "4.7.0"),
                new PackageIdentity("Microsoft.VisualBasic", "10.3.0")));
#endif

            return referenceAssemblies;
        }
    }
}
