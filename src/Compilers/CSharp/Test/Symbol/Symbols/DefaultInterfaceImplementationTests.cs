﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    [CompilerTrait(CompilerFeature.DefaultInterfaceImplementation)]
    public class DefaultInterfaceImplementationTests : CSharpTestBase
    {
        [Fact]
        public void MethodImplementation_011()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";
            ValidateMethodImplementation_011(source1);
        }

        private void ValidateMethodImplementation_011(string source1)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidateMethodImplementationTest1_011(m, "void I1.M1()");
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify:false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateMethodImplementationTest2_011(m, "void I1.M1()");
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            Validate2(compilation3.SourceModule);

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        private static void ValidateMethodImplementationTest1_011(ModuleSymbol m, string expectedImplementation)
        {
            var i1 = m.GlobalNamespace.GetTypeMember("I1");
            var m1 = i1.GetMember<MethodSymbol>("M1");

            Assert.True(i1.IsAbstract);
            Assert.True(i1.IsMetadataAbstract);
            Assert.True(m1.IsMetadataVirtual());
            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsStatic);
            Assert.False(m1.IsExtern);
            Assert.False(m1.IsAsync);
            Assert.False(m1.IsOverride);
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);

            if (m is PEModuleSymbol peModule)
            {
                int rva;
                peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)m1).Handle, out _, out _, out _, out rva);
                Assert.NotEqual(0, rva);
            }

            var test1 = m.GlobalNamespace.GetTypeMember("Test1");
            Assert.Equal(expectedImplementation, test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
            Assert.Equal("I1", test1.Interfaces.Single().ToTestDisplayString());
        }

        private static void ValidateMethodImplementationTest2_011(ModuleSymbol m, string expectedImplementation)
        {
            var test2 = m.GlobalNamespace.GetTypeMember("Test2");
            Assert.Equal("I1", test2.Interfaces.Single().ToTestDisplayString());
            var m1 = test2.Interfaces.Single().GetMember<MethodSymbol>("M1");

            Assert.Equal(expectedImplementation, test2.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
        }

        [Fact]
        public void MethodImplementation_012()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{
    public void M1() 
    {
        System.Console.WriteLine(""Test1 M1"");
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m) 
            {
                ValidateMethodImplementationTest1_011(m, "void Test1.M1()");
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{
    public void M1() 
    {
        System.Console.WriteLine(""Test2 M1"");
    }
}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateMethodImplementationTest2_011(m, "void Test2.M1()");
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);
            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void MethodImplementation_013()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{
    void I1.M1() 
    {
        System.Console.WriteLine(""Test1 M1"");
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidateMethodImplementationTest1_011(m, "void Test1.I1.M1()");
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{
    void I1.M1() 
    {
        System.Console.WriteLine(""Test2 M1"");
    }
}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateMethodImplementationTest2_011(m, "void Test2.I1.M1()");
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void MethodImplementation_021()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    void M2() {}
}

class Base
{
    void M1() { }
}

class Derived : Base, I1
{
    void M2() { }
}

class Test : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var m1 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M1");
                var m2 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M2");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Same(m1, derived.FindImplementationForInterfaceMember(m1));
                Assert.Same(m2, derived.FindImplementationForInterfaceMember(m2));
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void MethodImplementation_022()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    void M2() {}
}

class Base : Test
{
    void M1() { }
}

class Derived : Base, I1
{
    void M2() { }
}

class Test : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var m1 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M1");
                var m2 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M2");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Same(m1, derived.FindImplementationForInterfaceMember(m1));
                Assert.Same(m2, derived.FindImplementationForInterfaceMember(m2));
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void MethodImplementation_023()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    void M2() {}
}

class Base : Test
{
    void M1() { }
}

class Derived : Base, I1
{
    void M2() { }
}

class Test : I1 
{
    void I1.M1() {}
    void I1.M2() {}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var m1 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M1");
                var m2 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M2");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("void Test.I1.M1()", derived.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
                Assert.Equal("void Test.I1.M2()", derived.FindImplementationForInterfaceMember(m2).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void MethodImplementation_024()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    void M2() {}
}

class Base : Test
{
    new void M1() { }
}

class Derived : Base, I1
{
    new void M2() { }
}

class Test : I1 
{
    public void M1() {}
    public void M2() {}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var m1 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M1");
                var m2 = m.GlobalNamespace.GetMember<MethodSymbol>("I1.M2");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("void Test.M1()", derived.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
                Assert.Equal("void Test.M2()", derived.FindImplementationForInterfaceMember(m2).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void MethodImplementation_031()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
}

class Test1 : I1
{
    public static void M1() { }
}

class Test2 : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_032()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
}

class Test1 : Test2, I1
{
    public static void M1() { }
}

class Test2 : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_033()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
}

class Test1 : Test2, I1
{
    public static void M1() { }
}

class Test2 : I1 
{
    void I1.M1() {}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Equal("void Test2.I1.M1()", test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_034()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
}

class Test1 : Test2, I1
{
    new public static void M1() { }
}

class Test2 : I1 
{
    public void M1() {}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Equal("void Test2.M1()", test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_041()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    int M2() => 1; 
}

class Test1 : I1
{
    public int M1() { return 0; }
    public ref int M2() { throw null; }
}

class Test2 : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));
            Assert.Same(m2, test1.FindImplementationForInterfaceMember(m2));

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_042()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    int M2() => 1; 
}

class Test1 : Test2, I1
{
    public int M1() { return 0; }
    public ref int M2() { throw null; }
}

class Test2 : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));
            Assert.Same(m2, test1.FindImplementationForInterfaceMember(m2));

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_043()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    int M2() => 1; 
}

class Test1 : Test2, I1
{
    public int M1() { return 0; }
    public ref int M2() { throw null; }
}

class Test2 : I1 
{
    void I1.M1() {}
    int I1.M2() => 1; 
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Equal("void Test2.I1.M1()", test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
            Assert.Equal("System.Int32 Test2.I1.M2()", test1.FindImplementationForInterfaceMember(m2).ToTestDisplayString());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_044()
        {
            var source1 =
@"
interface I1
{
    void M1() {}
    int M2() => 1; 
}

class Test1 : Test2, I1
{
    new public int M1() { return 0; }
    new public ref int M2() { throw null; }
}

class Test2 : I1 
{
    public void M1() {}
    public int M2() => 1; 
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");
            var m2 = compilation1.GetMember<MethodSymbol>("I1.M2");

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Equal("void Test2.M1()", test1.FindImplementationForInterfaceMember(m1).ToTestDisplayString());
            Assert.Equal("System.Int32 Test2.M2()", test1.FindImplementationForInterfaceMember(m2).ToTestDisplayString());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var test1Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test1");
                    Assert.Equal("I1", test1Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        private static MetadataReference MscorlibRefWithoutSharingCachedSymbols
        {
            get
            {
                return ((AssemblyMetadata)((MetadataImageReference)MscorlibRef).GetMetadata()).CopyWithoutSharingCachedSymbols().
                    GetReference(display: "mscorlib.v4_0_30319.dll");
            }
        }

        [Fact]
        public void MethodImplementation_051()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new [] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics(
                // (4,10): error CS8501: Target runtime doesn't support default interface implementation.
                //     void M1() 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "M1").WithLocation(4, 10)
                );

            Assert.True(m1.IsMetadataVirtual());

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.M1()' cannot implement interface member 'I1.M1()' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.M1()", "I1.M1()", "Test2").WithLocation(2, 15)
                );
        }

        [Fact]
        public void MethodImplementation_052()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}
";

            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
class Test2 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;
            var m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.M1()' cannot implement interface member 'I1.M1()' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.M1()", "I1.M1()", "Test2").WithLocation(2, 15)
                );
        }

        [Fact]
        public void MethodImplementation_053()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}
";

            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
public interface I2
{
    void M2();
}

class Test2 : I2
{
    public void M2() {}
}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;
            var m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Null(test2.FindImplementationForInterfaceMember(m1));

            compilation3.VerifyDiagnostics();
        }

        [Fact]
        public void MethodImplementation_061()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics(
                // (4,10): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     void M1() 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "M1").WithArguments("default interface implementation", "7.1").WithLocation(4, 10),
                // (4,10): error CS8501: Target runtime doesn't support default interface implementation.
                //     void M1() 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "M1").WithLocation(4, 10)
                );

            Assert.True(m1.IsMetadataVirtual());

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.M1()' cannot implement interface member 'I1.M1()' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.M1()", "I1.M1()", "Test2").WithLocation(2, 15)
                );
        }

        [Fact]
        public void MethodImplementation_071()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics(
                // (4,10): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     void M1() 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "M1").WithArguments("default interface implementation", "7.1").WithLocation(4, 10)
                );

            Assert.True(m1.IsMetadataVirtual());

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation2.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation2.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation2.VerifyDiagnostics();

            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                });
        }

        [Fact]
        public void MethodImplementation_081()
        {
            var source1 =
@"
public interface I1
{
    I1 M1() 
    {
        throw null;
    }
}
";
            var compilation1 = CreateCompilation(source1, new[] { SystemCoreRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.False(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            compilation1.VerifyDiagnostics(
                // (4,8): error CS8501: Target runtime doesn't support default interface implementation.
                //     I1 M1() 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "M1").WithLocation(4, 8)
                );

            Assert.True(m1.IsMetadataVirtual());

            Assert.Throws<System.InvalidOperationException>(() => compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false);
        }

        [Fact]
        public void MethodImplementation_091()
        {
            var source1 =
@"
public interface I1
{
    static void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.False(m1.IsVirtual);
            Assert.True(m1.IsStatic);
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Null(test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics(
                // (4,17): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     static void M1() 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "M1").WithArguments("default interface implementation", "7.1").WithLocation(4, 17)
                );

            Assert.False(m1.IsMetadataVirtual());
        }

        [Fact]
        public void MethodImplementation_101()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

public interface I2 : I1
{}

class Test1 : I2
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            var m1 = compilation1.GetMember<MethodSymbol>("I1.M1");

            Assert.False(m1.IsAbstract);
            Assert.True(m1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));

            compilation1.VerifyDiagnostics();
            Assert.True(m1.IsMetadataVirtual());

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var i1 = m.GlobalNamespace.GetTypeMember("I1");
                    var result = (PEMethodSymbol)i1.GetMember("M1");

                    Assert.True(result.IsMetadataVirtual());
                    Assert.False(result.IsAbstract);
                    Assert.True(result.IsVirtual);
                    Assert.True(i1.IsAbstract);
                    Assert.True(i1.IsMetadataAbstract);

                    int rva;
                    ((PEModuleSymbol)m).Module.GetMethodDefPropsOrThrow(result.Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);

                    var test1Result = m.GlobalNamespace.GetTypeMember("Test1");
                    var interfaces = test1Result.Interfaces.ToArray();
                    Assert.Equal(2, interfaces.Length);
                    Assert.Equal("I2", interfaces[0].ToTestDisplayString());
                    Assert.Equal("I1", interfaces[1].ToTestDisplayString());
                });

            var source2 =
@"
class Test2 : I2
{}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation2.GetMember<MethodSymbol>("I1.M1");
            var test2 = compilation2.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    var interfaces = test2Result.Interfaces.ToArray();
                    Assert.Equal(2, interfaces.Length);
                    Assert.Equal("I2", interfaces[0].ToTestDisplayString());
                    Assert.Equal("I1", interfaces[1].ToTestDisplayString());
                });

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            m1 = compilation3.GetMember<MethodSymbol>("I1.M1");
            test2 = compilation3.GetTypeByMetadataName("Test2");

            Assert.Same(m1, test2.FindImplementationForInterfaceMember(m1));

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    var interfaces = test2Result.Interfaces.ToArray();
                    Assert.Equal(2, interfaces.Length);
                    Assert.Equal("I2", interfaces[0].ToTestDisplayString());
                    Assert.Equal("I1", interfaces[1].ToTestDisplayString());
                });
        }

        [Fact]
        public void PropertyImplementation_101()
        {
            var source1 =
@"
public interface I1
{
    int P1 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
    }
}

class Test1 : I1
{}
";
            ValidatePropertyImplementation_101(source1);
        }

        private void ValidatePropertyImplementation_101(string source1)
        {
            ValidatePropertyImplementation_101(source1, "P1", haveGet: true, haveSet: false);
        }

        private void ValidatePropertyImplementation_101(string source1, string propertyName, bool haveGet, bool haveSet)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidatePropertyImplementationTest1_101(m, propertyName, haveGet, haveSet);
            }

            Validate1(compilation1.SourceModule);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidatePropertyImplementationTest2_101(m, propertyName, haveGet, haveSet);
            }

            Validate2(compilation2.SourceModule);
            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);
            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        private static void ValidatePropertyImplementationTest1_101(ModuleSymbol m, string propertyName, bool haveGet, bool haveSet)
        {
            var i1 = m.GlobalNamespace.GetTypeMember("I1");
            var p1 = i1.GetMember<PropertySymbol>(propertyName);

            Assert.Equal(!haveSet, p1.IsReadOnly);
            Assert.Equal(!haveGet, p1.IsWriteOnly);

            if (haveGet)
            {
                ValidateAccessor(p1.GetMethod);
            }
            else
            {
                Assert.Null(p1.GetMethod);
            }

            if (haveSet)
            {
                ValidateAccessor(p1.SetMethod);
            }
            else
            {
                Assert.Null(p1.SetMethod);
            }

            void ValidateAccessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.True(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            Assert.True(i1.IsAbstract);
            Assert.True(i1.IsMetadataAbstract);

            if (m is PEModuleSymbol peModule)
            {
                int rva;

                if (haveGet)
                {
                    peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)p1.GetMethod).Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);
                }

                if (haveSet)
                {
                    peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)p1.SetMethod).Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);
                }
            }

            var test1 = m.GlobalNamespace.GetTypeMember("Test1");
            Assert.Equal("I1", test1.Interfaces.Single().ToTestDisplayString());
            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));

            if (haveGet)
            {
                Assert.Same(p1.GetMethod, test1.FindImplementationForInterfaceMember(p1.GetMethod));
            }

            if (haveSet)
            {
                Assert.Same(p1.SetMethod, test1.FindImplementationForInterfaceMember(p1.SetMethod));
            }
        }

        private static void ValidatePropertyImplementationTest2_101(ModuleSymbol m, string propertyName, bool haveGet, bool haveSet)
        {
            var test2 = m.GlobalNamespace.GetTypeMember("Test2");
            Assert.Equal("I1", test2.Interfaces.Single().ToTestDisplayString());

            var p1 = test2.Interfaces.Single().GetMember<PropertySymbol>(propertyName);
            Assert.Same(p1, test2.FindImplementationForInterfaceMember(p1));

            if (haveGet)
            {
                var getP1 = p1.GetMethod;
                Assert.Same(getP1, test2.FindImplementationForInterfaceMember(getP1));
            }

            if (haveSet)
            {
                var setP1 = p1.SetMethod;
                Assert.Same(setP1, test2.FindImplementationForInterfaceMember(setP1));
            }
        }

        [Fact]
        public void PropertyImplementation_102()
        {
            var source1 =
@"
public interface I1
{
    int P1 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
";
            ValidatePropertyImplementation_102(source1);
        }

        private void ValidatePropertyImplementation_102(string source1)
        {
            ValidatePropertyImplementation_101(source1, "P1", haveGet: true, haveSet: true);
        }

        [Fact]
        public void PropertyImplementation_103()
        {
            var source1 =
@"
public interface I1
{
    int P1 
    {
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
";
            ValidatePropertyImplementation_103(source1);
        }

        private void ValidatePropertyImplementation_103(string source1)
        {
            ValidatePropertyImplementation_101(source1, "P1", haveGet: false, haveSet: true);
        }

        [Fact]
        public void PropertyImplementation_104()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 0;
}

class Test1 : I1
{}
";
            ValidatePropertyImplementation_101(source1);
        }

        [Fact]
        public void PropertyImplementation_105()
        {
            var source1 =
@"
public interface I1
{
    int P1 {add; remove;} => 0;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,16): error CS0073: An add or remove accessor must have a body
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 16),
                // (4,24): error CS0073: An add or remove accessor must have a body
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 24),
                // (4,13): error CS1014: A get or set accessor expected
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "add").WithLocation(4, 13),
                // (4,18): error CS1014: A get or set accessor expected
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "remove").WithLocation(4, 18),
                // (4,9): error CS0548: 'I1.P1': property or indexer must have at least one accessor
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P1").WithArguments("I1.P1").WithLocation(4, 9),
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     int P1 {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "int P1 {add; remove;} => 0;").WithLocation(4, 5)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            Assert.True(p1.IsAbstract);
            Assert.Null(p1.GetMethod);
            Assert.Null(p1.SetMethod);
            Assert.True(p1.IsReadOnly);
            Assert.True(p1.IsWriteOnly);
        }

        [Fact]
        public void PropertyImplementation_106()
        {
            var source1 =
@"
public interface I1
{
    int P1 {get; set;} => 0;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     int P1 {get; set;} => 0;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "int P1 {get; set;} => 0;").WithLocation(4, 5)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            Assert.True(p1.IsAbstract);
            Assert.True(p1.GetMethod.IsAbstract);
            Assert.True(p1.SetMethod.IsAbstract);
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);
        }

        [Fact]
        public void PropertyImplementation_107()
        {
            var source1 =
@"
public interface I1
{
    int P1 {add; remove;} = 0;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,16): error CS0073: An add or remove accessor must have a body
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 16),
                // (4,24): error CS0073: An add or remove accessor must have a body
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 24),
                // (4,13): error CS1014: A get or set accessor expected
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "add").WithLocation(4, 13),
                // (4,18): error CS1014: A get or set accessor expected
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "remove").WithLocation(4, 18),
                // (4,9): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P1").WithArguments("I1.P1").WithLocation(4, 9),
                // (4,9): error CS0548: 'I1.P1': property or indexer must have at least one accessor
                //     int P1 {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P1").WithArguments("I1.P1").WithLocation(4, 9)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            Assert.True(p1.IsAbstract);
            Assert.Null(p1.GetMethod);
            Assert.Null(p1.SetMethod);
            Assert.True(p1.IsReadOnly);
            Assert.True(p1.IsWriteOnly);
        }

        [Fact]
        public void PropertyImplementation_108()
        {
            var source1 =
@"
public interface I1
{
    int P1 {get; set;} = 0;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,9): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     int P1 {get; set;} = 0;
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P1").WithArguments("I1.P1").WithLocation(4, 9)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            Assert.True(p1.IsAbstract);
            Assert.True(p1.GetMethod.IsAbstract);
            Assert.True(p1.SetMethod.IsAbstract);
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);
        }

        [Fact]
        public void PropertyImplementation_109()
        {
            var source1 =
@"
public interface I1
{
    int P1 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
        set;
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            // PROTOTYPE(DefaultInterfaceImplementation): We might want to allow code like this.
            compilation1.VerifyDiagnostics(
                // (11,9): error CS0501: 'I1.P1.set' must declare a body because it is not marked abstract, extern, or partial
                //         set;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I1.P1.set").WithLocation(11, 9)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            var getP1 = p1.GetMethod;
            var setP1 = p1.SetMethod;
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.False(getP1.IsAbstract);
            Assert.True(getP1.IsVirtual);
            Assert.False(setP1.IsAbstract);
            Assert.True(setP1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(getP1, test1.FindImplementationForInterfaceMember(getP1));
            Assert.Same(setP1, test1.FindImplementationForInterfaceMember(setP1));

            Assert.True(getP1.IsMetadataVirtual());
            Assert.True(setP1.IsMetadataVirtual());
        }

        [Fact]
        public void PropertyImplementation_110()
        {
            var source1 =
@"
public interface I1
{
    int P1 
    {
        get;
        set => System.Console.WriteLine(""set P1"");
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            // PROTOTYPE(DefaultInterfaceImplementation): We might want to allow code like this.
            compilation1.VerifyDiagnostics(
                // (6,9): error CS0501: 'I1.P1.get' must declare a body because it is not marked abstract, extern, or partial
                //         get;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P1.get").WithLocation(6, 9)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.P1");
            var getP1 = p1.GetMethod;
            var setP1 = p1.SetMethod;
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.False(getP1.IsAbstract);
            Assert.True(getP1.IsVirtual);
            Assert.False(setP1.IsAbstract);
            Assert.True(setP1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(getP1, test1.FindImplementationForInterfaceMember(getP1));
            Assert.Same(setP1, test1.FindImplementationForInterfaceMember(setP1));

            Assert.True(getP1.IsMetadataVirtual());
            Assert.True(setP1.IsMetadataVirtual());
        }

        [Fact]
        public void PropertyImplementation_201()
        {
            var source1 =
@"
interface I1
{
    int P1 => 1;
    int P2 => 2;
    int P3 { get => 3; }
    int P4 { get => 4; }
    int P5 { set => System.Console.WriteLine(5); }
    int P6 { set => System.Console.WriteLine(6); }
    int P7 { get { return 7;} set {} }
    int P8 { get { return 8;} set {} }
}

class Base
{
    int P1 => 10;
    int P3 { get => 30; }
    int P5 { set => System.Console.WriteLine(50); }
    int P7 { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int P2 => 20;
    int P4 { get => 40; }
    int P6 { set => System.Console.WriteLine(60); }
    int P8 { get { return 80;} set {} }
}

class Test : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            ValidatePropertyImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidatePropertyImplementation_201(m);
                });
        }

        private static void ValidatePropertyImplementation_201(ModuleSymbol m)
        {
            var p1 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P1");
            var p2 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P2");
            var p3 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P3");
            var p4 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P4");
            var p5 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P5");
            var p6 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P6");
            var p7 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P7");
            var p8 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P8");

            var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

            Assert.Same(p1, derived.FindImplementationForInterfaceMember(p1));
            Assert.Same(p2, derived.FindImplementationForInterfaceMember(p2));
            Assert.Same(p3, derived.FindImplementationForInterfaceMember(p3));
            Assert.Same(p4, derived.FindImplementationForInterfaceMember(p4));
            Assert.Same(p5, derived.FindImplementationForInterfaceMember(p5));
            Assert.Same(p6, derived.FindImplementationForInterfaceMember(p6));
            Assert.Same(p7, derived.FindImplementationForInterfaceMember(p7));
            Assert.Same(p8, derived.FindImplementationForInterfaceMember(p8));

            Assert.Same(p1.GetMethod, derived.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Same(p2.GetMethod, derived.FindImplementationForInterfaceMember(p2.GetMethod));
            Assert.Same(p3.GetMethod, derived.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Same(p4.GetMethod, derived.FindImplementationForInterfaceMember(p4.GetMethod));
            Assert.Same(p5.SetMethod, derived.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Same(p6.SetMethod, derived.FindImplementationForInterfaceMember(p6.SetMethod));
            Assert.Same(p7.GetMethod, derived.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Same(p8.GetMethod, derived.FindImplementationForInterfaceMember(p8.GetMethod));
            Assert.Same(p7.SetMethod, derived.FindImplementationForInterfaceMember(p7.SetMethod));
            Assert.Same(p8.SetMethod, derived.FindImplementationForInterfaceMember(p8.SetMethod));
        }

        [Fact]
        public void PropertyImplementation_202()
        {
            var source1 =
@"
interface I1
{
    int P1 => 1;
    int P2 => 2;
    int P3 { get => 3; }
    int P4 { get => 4; }
    int P5 { set => System.Console.WriteLine(5); }
    int P6 { set => System.Console.WriteLine(6); }
    int P7 { get { return 7;} set {} }
    int P8 { get { return 8;} set {} }
}

class Base : Test
{
    int P1 => 10;
    int P3 { get => 30; }
    int P5 { set => System.Console.WriteLine(50); }
    int P7 { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int P2 => 20;
    int P4 { get => 40; }
    int P6 { set => System.Console.WriteLine(60); }
    int P8 { get { return 80;} set {} }
}

class Test : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            ValidatePropertyImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidatePropertyImplementation_201(m);
                });
        }

        [Fact]
        public void PropertyImplementation_203()
        {
            var source1 =
@"
interface I1
{
    int P1 => 1;
    int P2 => 2;
    int P3 { get => 3; }
    int P4 { get => 4; }
    int P5 { set => System.Console.WriteLine(5); }
    int P6 { set => System.Console.WriteLine(6); }
    int P7 { get { return 7;} set {} }
    int P8 { get { return 8;} set {} }
}

class Base : Test
{
    int P1 => 10;
    int P3 { get => 30; }
    int P5 { set => System.Console.WriteLine(50); }
    int P7 { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int P2 => 20;
    int P4 { get => 40; }
    int P6 { set => System.Console.WriteLine(60); }
    int P8 { get { return 80;} set {} }
}

class Test : I1 
{
    int I1.P1 => 100;
    int I1.P2 => 200;
    int I1.P3 { get => 300; }
    int I1.P4 { get => 400; }
    int I1.P5 { set => System.Console.WriteLine(500); }
    int I1.P6 { set => System.Console.WriteLine(600); }
    int I1.P7 { get { return 700;} set {} }
    int I1.P8 { get { return 800;} set {} }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var p1 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P1");
                var p2 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P2");
                var p3 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P3");
                var p4 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P4");
                var p5 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P5");
                var p6 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P6");
                var p7 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P7");
                var p8 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P8");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("System.Int32 Test.I1.P1 { get; }", derived.FindImplementationForInterfaceMember(p1).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P2 { get; }", derived.FindImplementationForInterfaceMember(p2).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P3 { get; }", derived.FindImplementationForInterfaceMember(p3).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P4 { get; }", derived.FindImplementationForInterfaceMember(p4).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P5 { set; }", derived.FindImplementationForInterfaceMember(p5).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P6 { set; }", derived.FindImplementationForInterfaceMember(p6).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P7 { get; set; }", derived.FindImplementationForInterfaceMember(p7).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P8 { get; set; }", derived.FindImplementationForInterfaceMember(p8).ToTestDisplayString());

                Assert.Equal("System.Int32 Test.I1.P1.get", derived.FindImplementationForInterfaceMember(p1.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P2.get", derived.FindImplementationForInterfaceMember(p2.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P3.get", derived.FindImplementationForInterfaceMember(p3.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P4.get", derived.FindImplementationForInterfaceMember(p4.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.P5.set", derived.FindImplementationForInterfaceMember(p5.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.P6.set", derived.FindImplementationForInterfaceMember(p6.SetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P7.get", derived.FindImplementationForInterfaceMember(p7.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1.P8.get", derived.FindImplementationForInterfaceMember(p8.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.P7.set", derived.FindImplementationForInterfaceMember(p7.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.P8.set", derived.FindImplementationForInterfaceMember(p8.SetMethod).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void PropertyImplementation_204()
        {
            var source1 =
@"
interface I1
{
    int P1 => 1;
    int P2 => 2;
    int P3 { get => 3; }
    int P4 { get => 4; }
    int P5 { set => System.Console.WriteLine(5); }
    int P6 { set => System.Console.WriteLine(6); }
    int P7 { get { return 7;} set {} }
    int P8 { get { return 8;} set {} }
}

class Base : Test
{
    new int P1 => 10;
    new int P3 { get => 30; }
    new int P5 { set => System.Console.WriteLine(50); }
    new int P7 { get { return 70;} set {} }
}

class Derived : Base, I1
{
    new int P2 => 20;
    new int P4 { get => 40; }
    new int P6 { set => System.Console.WriteLine(60); }
    new int P8 { get { return 80;} set {} }
}

class Test : I1 
{
    public int P1 => 100;
    public int P2 => 200;
    public int P3 { get => 300; }
    public int P4 { get => 400; }
    public int P5 { set => System.Console.WriteLine(500); }
    public int P6 { set => System.Console.WriteLine(600); }
    public int P7 { get { return 700;} set {} }
    public int P8 { get { return 800;} set {} }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var p1 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P1");
                var p2 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P2");
                var p3 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P3");
                var p4 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P4");
                var p5 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P5");
                var p6 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P6");
                var p7 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P7");
                var p8 = m.GlobalNamespace.GetMember<PropertySymbol>("I1.P8");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("System.Int32 Test.P1 { get; }", derived.FindImplementationForInterfaceMember(p1).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P2 { get; }", derived.FindImplementationForInterfaceMember(p2).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P3 { get; }", derived.FindImplementationForInterfaceMember(p3).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P4 { get; }", derived.FindImplementationForInterfaceMember(p4).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P5 { set; }", derived.FindImplementationForInterfaceMember(p5).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P6 { set; }", derived.FindImplementationForInterfaceMember(p6).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P7 { get; set; }", derived.FindImplementationForInterfaceMember(p7).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P8 { get; set; }", derived.FindImplementationForInterfaceMember(p8).ToTestDisplayString());

                Assert.Equal("System.Int32 Test.P1.get", derived.FindImplementationForInterfaceMember(p1.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P2.get", derived.FindImplementationForInterfaceMember(p2.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P3.get", derived.FindImplementationForInterfaceMember(p3.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P4.get", derived.FindImplementationForInterfaceMember(p4.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.P5.set", derived.FindImplementationForInterfaceMember(p5.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.P6.set", derived.FindImplementationForInterfaceMember(p6.SetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P7.get", derived.FindImplementationForInterfaceMember(p7.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.P8.get", derived.FindImplementationForInterfaceMember(p8.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.P7.set", derived.FindImplementationForInterfaceMember(p7.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.P8.set", derived.FindImplementationForInterfaceMember(p8.SetMethod).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void PropertyImplementation_501()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 1;
    int P3 
    { get => 3; }
    int P5 
    { set => System.Console.WriteLine(5); }
    int P7 
    { 
        get { return 7;} 
        set {} 
    }
}

class Test1 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;
            compilation1.VerifyDiagnostics(
                // (4,15): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P1 => 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "1").WithLocation(4, 15),
                // (6,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(6, 7),
                // (8,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(8, 7),
                // (11,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(11, 9),
                // (12,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         set {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(12, 9)
                );

            ValidatePropertyImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.P7.set' cannot implement interface member 'I1.P7.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.set", "I1.P7.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P1.get' cannot implement interface member 'I1.P1.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P1.get", "I1.P1.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P3.get' cannot implement interface member 'I1.P3.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P3.get", "I1.P3.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P5.set' cannot implement interface member 'I1.P5.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P5.set", "I1.P5.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P7.get' cannot implement interface member 'I1.P7.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.get", "I1.P7.get", "Test2").WithLocation(2, 15)
                );

            ValidatePropertyImplementation_501(compilation3.SourceModule, "Test2");
        }

        private static void ValidatePropertyImplementation_501(ModuleSymbol m, string typeName)
        {
            var derived = m.GlobalNamespace.GetTypeMember(typeName);
            var i1 = derived.Interfaces.Single();
            Assert.Equal("I1", i1.ToTestDisplayString());

            var p1 = i1.GetMember<PropertySymbol>("P1");
            var p3 = i1.GetMember<PropertySymbol>("P3");
            var p5 = i1.GetMember<PropertySymbol>("P5");
            var p7 = i1.GetMember<PropertySymbol>("P7");

            Assert.True(p1.IsVirtual);
            Assert.True(p3.IsVirtual);
            Assert.True(p5.IsVirtual);
            Assert.True(p7.IsVirtual);

            Assert.False(p1.IsAbstract);
            Assert.False(p3.IsAbstract);
            Assert.False(p5.IsAbstract);
            Assert.False(p7.IsAbstract);

            Assert.Same(p1, derived.FindImplementationForInterfaceMember(p1));
            Assert.Same(p3, derived.FindImplementationForInterfaceMember(p3));
            Assert.Same(p5, derived.FindImplementationForInterfaceMember(p5));
            Assert.Same(p7, derived.FindImplementationForInterfaceMember(p7));

            Assert.True(p1.GetMethod.IsVirtual);
            Assert.True(p3.GetMethod.IsVirtual);
            Assert.True(p5.SetMethod.IsVirtual);
            Assert.True(p7.GetMethod.IsVirtual);
            Assert.True(p7.SetMethod.IsVirtual);

            Assert.True(p1.GetMethod.IsMetadataVirtual());
            Assert.True(p3.GetMethod.IsMetadataVirtual());
            Assert.True(p5.SetMethod.IsMetadataVirtual());
            Assert.True(p7.GetMethod.IsMetadataVirtual());
            Assert.True(p7.SetMethod.IsMetadataVirtual());

            Assert.False(p1.GetMethod.IsAbstract);
            Assert.False(p3.GetMethod.IsAbstract);
            Assert.False(p5.SetMethod.IsAbstract);
            Assert.False(p7.GetMethod.IsAbstract);
            Assert.False(p7.SetMethod.IsAbstract);

            Assert.Same(p1.GetMethod, derived.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Same(p3.GetMethod, derived.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Same(p5.SetMethod, derived.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Same(p7.GetMethod, derived.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Same(p7.SetMethod, derived.FindImplementationForInterfaceMember(p7.SetMethod));
        }

        [Fact]
        public void PropertyImplementation_502()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 1;
    int P3 { get => 3; }
    int P5 { set => System.Console.WriteLine(5); }
    int P7 { get { return 7;} set {} }
}
";

            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
class Test2 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.P7.set' cannot implement interface member 'I1.P7.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.set", "I1.P7.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P1.get' cannot implement interface member 'I1.P1.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P1.get", "I1.P1.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P3.get' cannot implement interface member 'I1.P3.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P3.get", "I1.P3.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P5.set' cannot implement interface member 'I1.P5.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P5.set", "I1.P5.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P7.get' cannot implement interface member 'I1.P7.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.get", "I1.P7.get", "Test2").WithLocation(2, 15)
                );

            ValidatePropertyImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void PropertyImplementation_503()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 1;
    int P3 { get => 3; }
    int P5 { set => System.Console.WriteLine(5); }
    int P7 { get { return 7;} set {} }
}
";

            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
public interface I2
{
    void M2();
}

class Test2 : I2
{
    public void M2() {}
}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            var test2 = compilation3.GetTypeByMetadataName("Test2");
            var i1 = compilation3.GetTypeByMetadataName("I1");
            Assert.Equal("I1", i1.ToTestDisplayString());

            var p1 = i1.GetMember<PropertySymbol>("P1");
            var p3 = i1.GetMember<PropertySymbol>("P3");
            var p5 = i1.GetMember<PropertySymbol>("P5");
            var p7 = i1.GetMember<PropertySymbol>("P7");

            Assert.Null(test2.FindImplementationForInterfaceMember(p1));
            Assert.Null(test2.FindImplementationForInterfaceMember(p3));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7));

            Assert.Null(test2.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7.SetMethod));

            compilation3.VerifyDiagnostics();
        }

        [Fact]
        public void PropertyImplementation_601()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 1;
    int P3 { get => 3; }
    int P5 { set => System.Console.WriteLine(5); }
    int P7 { get { return 7;} set {} }
}

class Test1 : I1
{}
";
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation1.VerifyDiagnostics(
                // (4,15): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P1 => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 15),
                // (4,15): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P1 => 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "1").WithLocation(4, 15),
                // (5,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P3 { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(5, 14),
                // (5,14): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P3 { get => 3; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(5, 14),
                // (6,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P5 { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(6, 14),
                // (6,14): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P5 { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(6, 14),
                // (7,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(7, 14),
                // (7,14): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(7, 14),
                // (7,31): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(7, 31),
                // (7,31): error CS8501: Target runtime doesn't support default interface implementation.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(7, 31)
                );

            ValidatePropertyImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.P7.set' cannot implement interface member 'I1.P7.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.set", "I1.P7.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P1.get' cannot implement interface member 'I1.P1.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P1.get", "I1.P1.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P3.get' cannot implement interface member 'I1.P3.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P3.get", "I1.P3.get", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P5.set' cannot implement interface member 'I1.P5.set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P5.set", "I1.P5.set", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.P7.get' cannot implement interface member 'I1.P7.get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.P7.get", "I1.P7.get", "Test2").WithLocation(2, 15)
                );

            ValidatePropertyImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void PropertyImplementation_701()
        {
            var source1 =
@"
public interface I1
{
    int P1 => 1;
    int P3 { get => 3; }
    int P5 { set => System.Console.WriteLine(5); }
    int P7 { get { return 7;} set {} }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,15): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P1 => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 15),
                // (5,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P3 { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(5, 14),
                // (6,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P5 { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(6, 14),
                // (7,14): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(7, 14),
                // (7,31): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(7, 31)
                );

            ValidatePropertyImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            ValidatePropertyImplementation_501(compilation2.SourceModule, "Test2");

            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                    ValidatePropertyImplementation_501(m, "Test2");
                });
        }

        [Fact]
        public void PropertyImplementation_901()
        {
            var source1 =
@"
public interface I1
{
    static int P1 => 1;
    static int P3 { get => 3; }
    static int P5 { set => System.Console.WriteLine(5); }
    static int P7 { get { return 7;} set {} }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,22): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     static int P1 => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 22),
                // (5,21): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     static int P3 { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(5, 21),
                // (6,21): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     static int P5 { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(6, 21),
                // (7,21): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     static int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(7, 21),
                // (7,38): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     static int P7 { get { return 7;} set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(7, 38)
                );

            var derived = compilation1.SourceModule.GlobalNamespace.GetTypeMember("Test1");
            var i1 = derived.Interfaces.Single();
            Assert.Equal("I1", i1.ToTestDisplayString());

            var p1 = i1.GetMember<PropertySymbol>("P1");
            var p3 = i1.GetMember<PropertySymbol>("P3");
            var p5 = i1.GetMember<PropertySymbol>("P5");
            var p7 = i1.GetMember<PropertySymbol>("P7");

            Assert.True(p1.IsStatic);
            Assert.True(p3.IsStatic);
            Assert.True(p5.IsStatic);
            Assert.True(p7.IsStatic);

            Assert.False(p1.IsVirtual);
            Assert.False(p3.IsVirtual);
            Assert.False(p5.IsVirtual);
            Assert.False(p7.IsVirtual);

            Assert.False(p1.IsAbstract);
            Assert.False(p3.IsAbstract);
            Assert.False(p5.IsAbstract);
            Assert.False(p7.IsAbstract);

            Assert.Null(derived.FindImplementationForInterfaceMember(p1));
            Assert.Null(derived.FindImplementationForInterfaceMember(p3));
            Assert.Null(derived.FindImplementationForInterfaceMember(p5));
            Assert.Null(derived.FindImplementationForInterfaceMember(p7));

            Assert.True(p1.GetMethod.IsStatic);
            Assert.True(p3.GetMethod.IsStatic);
            Assert.True(p5.SetMethod.IsStatic);
            Assert.True(p7.GetMethod.IsStatic);
            Assert.True(p7.SetMethod.IsStatic);

            Assert.False(p1.GetMethod.IsVirtual);
            Assert.False(p3.GetMethod.IsVirtual);
            Assert.False(p5.SetMethod.IsVirtual);
            Assert.False(p7.GetMethod.IsVirtual);
            Assert.False(p7.SetMethod.IsVirtual);

            Assert.False(p1.GetMethod.IsMetadataVirtual());
            Assert.False(p3.GetMethod.IsMetadataVirtual());
            Assert.False(p5.SetMethod.IsMetadataVirtual());
            Assert.False(p7.GetMethod.IsMetadataVirtual());
            Assert.False(p7.SetMethod.IsMetadataVirtual());

            Assert.False(p1.GetMethod.IsAbstract);
            Assert.False(p3.GetMethod.IsAbstract);
            Assert.False(p5.SetMethod.IsAbstract);
            Assert.False(p7.GetMethod.IsAbstract);
            Assert.False(p7.SetMethod.IsAbstract);

            Assert.Null(derived.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Null(derived.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Null(derived.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Null(derived.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Null(derived.FindImplementationForInterfaceMember(p7.SetMethod));
        }

        [Fact]
        public void IndexerImplementation_101()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
    }
}

class Test1 : I1
{}
";
            ValidateIndexerImplementation_101(source1);
        }

        private void ValidateIndexerImplementation_101(string source1)
        {
            ValidatePropertyImplementation_101(source1, "this[]", haveGet: true, haveSet: false);
        }

        [Fact]
        public void IndexerImplementation_102()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
";
            ValidateIndexerImplementation_102(source1);
        }

        private void ValidateIndexerImplementation_102(string source1)
        {
            ValidatePropertyImplementation_101(source1, "this[]", haveGet: true, haveSet: true);
        }

        [Fact]
        public void IndexerImplementation_103()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] 
    {
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
";
            ValidateIndexerImplementation_103(source1);
        }

        private void ValidateIndexerImplementation_103(string source1)
        {
            ValidatePropertyImplementation_101(source1, "this[]", haveGet: false, haveSet: true);
        }

        [Fact]
        public void IndexerImplementation_104()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] => 0;
}

class Test1 : I1
{}
";
            ValidateIndexerImplementation_101(source1);
        }

        [Fact]
        public void IndexerImplementation_105()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] {add; remove;} => 0;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,25): error CS0073: An add or remove accessor must have a body
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 25),
                // (4,33): error CS0073: An add or remove accessor must have a body
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 33),
                // (4,22): error CS1014: A get or set accessor expected
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "add").WithLocation(4, 22),
                // (4,27): error CS1014: A get or set accessor expected
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "remove").WithLocation(4, 27),
                // (4,9): error CS0548: 'I1.this[int]': property or indexer must have at least one accessor
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "this").WithArguments("I1.this[int]").WithLocation(4, 9),
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     int this[int i] {add; remove;} => 0;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "int this[int i] {add; remove;} => 0;").WithLocation(4, 5)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            Assert.True(p1.IsAbstract);
            Assert.Null(p1.GetMethod);
            Assert.Null(p1.SetMethod);
            Assert.True(p1.IsReadOnly);
            Assert.True(p1.IsWriteOnly);
        }

        [Fact]
        public void IndexerImplementation_106()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] {get; set;} => 0;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     int this[int i] {get; set;} => 0;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "int this[int i] {get; set;} => 0;").WithLocation(4, 5)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            Assert.True(p1.IsAbstract);
            Assert.True(p1.GetMethod.IsAbstract);
            Assert.True(p1.SetMethod.IsAbstract);
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);
        }

        [Fact]
        public void IndexerImplementation_107()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] {add; remove;} = 0;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,25): error CS0073: An add or remove accessor must have a body
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 25),
                // (4,33): error CS0073: An add or remove accessor must have a body
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(4, 33),
                // (4,36): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(4, 36),
                // (4,22): error CS1014: A get or set accessor expected
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "add").WithLocation(4, 22),
                // (4,27): error CS1014: A get or set accessor expected
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_GetOrSetExpected, "remove").WithLocation(4, 27),
                // (4,9): error CS0548: 'I1.this[int]': property or indexer must have at least one accessor
                //     int this[int i] {add; remove;} = 0;
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "this").WithArguments("I1.this[int]").WithLocation(4, 9)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            Assert.True(p1.IsAbstract);
            Assert.Null(p1.GetMethod);
            Assert.Null(p1.SetMethod);
            Assert.True(p1.IsReadOnly);
            Assert.True(p1.IsWriteOnly);
        }

        [Fact]
        public void IndexerImplementation_108()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] {get; set;} = 0;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,33): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     int this[int i] {get; set;} = 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(4, 33)
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            Assert.True(p1.IsAbstract);
            Assert.True(p1.GetMethod.IsAbstract);
            Assert.True(p1.SetMethod.IsAbstract);
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);
        }

        [Fact]
        public void IndexerImplementation_109()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
        set;
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            // PROTOTYPE(DefaultInterfaceImplementation): We might want to allow code like this.
            compilation1.VerifyDiagnostics(
                // (11,9): error CS0501: 'I1.this[int].set' must declare a body because it is not marked abstract, extern, or partial
                //         set;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I1.this[int].set")
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            var getP1 = p1.GetMethod;
            var setP1 = p1.SetMethod;
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.False(getP1.IsAbstract);
            Assert.True(getP1.IsVirtual);
            Assert.False(setP1.IsAbstract);
            Assert.True(setP1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(getP1, test1.FindImplementationForInterfaceMember(getP1));
            Assert.Same(setP1, test1.FindImplementationForInterfaceMember(setP1));

            Assert.True(getP1.IsMetadataVirtual());
            Assert.True(setP1.IsMetadataVirtual());
        }

        [Fact]
        public void IndexerImplementation_110()
        {
            var source1 =
@"
public interface I1
{
    int this[int i] 
    {
        get;
        set => System.Console.WriteLine(""set P1"");
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            // PROTOTYPE(DefaultInterfaceImplementation): We might want to allow code like this.
            compilation1.VerifyDiagnostics(
                // (6,9): error CS0501: 'I1.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                //         get;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.this[int].get")
                );

            var p1 = compilation1.GetMember<PropertySymbol>("I1.this[]");
            var getP1 = p1.GetMethod;
            var setP1 = p1.SetMethod;
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.False(getP1.IsAbstract);
            Assert.True(getP1.IsVirtual);
            Assert.False(setP1.IsAbstract);
            Assert.True(setP1.IsVirtual);

            var test1 = compilation1.GetTypeByMetadataName("Test1");

            Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(getP1, test1.FindImplementationForInterfaceMember(getP1));
            Assert.Same(setP1, test1.FindImplementationForInterfaceMember(setP1));

            Assert.True(getP1.IsMetadataVirtual());
            Assert.True(setP1.IsMetadataVirtual());
        }

        [Fact]
        public void IndexerImplementation_201()
        {
            var source1 =
@"
interface I1
{
    int this[sbyte i] => 1;
    int this[byte i] => 2;
    int this[short i] { get => 3; }
    int this[ushort i] { get => 4; }
    int this[int i] { set => System.Console.WriteLine(5); }
    int this[uint i] { set => System.Console.WriteLine(6); }
    int this[long i] { get { return 7;} set {} }
    int this[ulong i] { get { return 8;} set {} }
}

class Base
{
    int this[sbyte i] => 10;
    int this[short i] { get => 30; }
    int this[int i] { set => System.Console.WriteLine(50); }
    int this[long i] { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int this[byte i] => 20;
    int this[ushort i] { get => 40; }
    int this[uint i] { set => System.Console.WriteLine(60); }
    int this[ulong i] { get { return 80;} set {} }
}

class Test : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            ValidateIndexerImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidateIndexerImplementation_201(m);
                });
        }

        private static void ValidateIndexerImplementation_201(ModuleSymbol m)
        {
            var i1 = m.GlobalNamespace.GetTypeMember("I1");
            var indexers = i1.GetMembers("this[]");
            var p1 = (PropertySymbol)indexers[0];
            var p2 = (PropertySymbol)indexers[1];
            var p3 = (PropertySymbol)indexers[2];
            var p4 = (PropertySymbol)indexers[3];
            var p5 = (PropertySymbol)indexers[4];
            var p6 = (PropertySymbol)indexers[5];
            var p7 = (PropertySymbol)indexers[6];
            var p8 = (PropertySymbol)indexers[7];

            var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

            Assert.Same(p1, derived.FindImplementationForInterfaceMember(p1));
            Assert.Same(p2, derived.FindImplementationForInterfaceMember(p2));
            Assert.Same(p3, derived.FindImplementationForInterfaceMember(p3));
            Assert.Same(p4, derived.FindImplementationForInterfaceMember(p4));
            Assert.Same(p5, derived.FindImplementationForInterfaceMember(p5));
            Assert.Same(p6, derived.FindImplementationForInterfaceMember(p6));
            Assert.Same(p7, derived.FindImplementationForInterfaceMember(p7));
            Assert.Same(p8, derived.FindImplementationForInterfaceMember(p8));

            Assert.Same(p1.GetMethod, derived.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Same(p2.GetMethod, derived.FindImplementationForInterfaceMember(p2.GetMethod));
            Assert.Same(p3.GetMethod, derived.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Same(p4.GetMethod, derived.FindImplementationForInterfaceMember(p4.GetMethod));
            Assert.Same(p5.SetMethod, derived.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Same(p6.SetMethod, derived.FindImplementationForInterfaceMember(p6.SetMethod));
            Assert.Same(p7.GetMethod, derived.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Same(p8.GetMethod, derived.FindImplementationForInterfaceMember(p8.GetMethod));
            Assert.Same(p7.SetMethod, derived.FindImplementationForInterfaceMember(p7.SetMethod));
            Assert.Same(p8.SetMethod, derived.FindImplementationForInterfaceMember(p8.SetMethod));
        }

        [Fact]
        public void IndexerImplementation_202()
        {
            var source1 =
@"
interface I1
{
    int this[sbyte i] => 1;
    int this[byte i] => 2;
    int this[short i] { get => 3; }
    int this[ushort i] { get => 4; }
    int this[int i] { set => System.Console.WriteLine(5); }
    int this[uint i] { set => System.Console.WriteLine(6); }
    int this[long i] { get { return 7;} set {} }
    int this[ulong i] { get { return 8;} set {} }
}

class Base : Test
{
    int this[sbyte i] => 10;
    int this[short i] { get => 30; }
    int this[int i] { set => System.Console.WriteLine(50); }
    int this[long i] { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int this[byte i] => 20;
    int this[ushort i] { get => 40; }
    int this[uint i] { set => System.Console.WriteLine(60); }
    int this[ulong i] { get { return 80;} set {} }
}

class Test : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            ValidateIndexerImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidateIndexerImplementation_201(m);
                });
        }

        [Fact]
        public void IndexerImplementation_203()
        {
            var source1 =
@"
interface I1
{
    int this[sbyte i] => 1;
    int this[byte i] => 2;
    int this[short i] { get => 3; }
    int this[ushort i] { get => 4; }
    int this[int i] { set => System.Console.WriteLine(5); }
    int this[uint i] { set => System.Console.WriteLine(6); }
    int this[long i] { get { return 7;} set {} }
    int this[ulong i] { get { return 8;} set {} }
}

class Base : Test
{
    int this[sbyte i] => 10;
    int this[short i] { get => 30; }
    int this[int i] { set => System.Console.WriteLine(50); }
    int this[long i] { get { return 70;} set {} }
}

class Derived : Base, I1
{
    int this[byte i] => 20;
    int this[ushort i] { get => 40; }
    int this[uint i] { set => System.Console.WriteLine(60); }
    int this[ulong i] { get { return 80;} set {} }
}

class Test : I1 
{
    int I1.this[sbyte i] => 100;
    int I1.this[byte i] => 200;
    int I1.this[short i] { get => 300; }
    int I1.this[ushort i] { get => 400; }
    int I1.this[int i] { set => System.Console.WriteLine(500); }
    int I1.this[uint i] { set => System.Console.WriteLine(600); }
    int I1.this[long i] { get { return 700;} set {} }
    int I1.this[ulong i] { get { return 800;} set {} }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var i1 = m.GlobalNamespace.GetTypeMember("I1");
                var indexers = i1.GetMembers("this[]");
                var p1 = (PropertySymbol)indexers[0];
                var p2 = (PropertySymbol)indexers[1];
                var p3 = (PropertySymbol)indexers[2];
                var p4 = (PropertySymbol)indexers[3];
                var p5 = (PropertySymbol)indexers[4];
                var p6 = (PropertySymbol)indexers[5];
                var p7 = (PropertySymbol)indexers[6];
                var p8 = (PropertySymbol)indexers[7];

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                string name = m is PEModuleSymbol ? "Item" : "this";

                Assert.Equal("System.Int32 Test.I1." + name + "[System.SByte i] { get; }", derived.FindImplementationForInterfaceMember(p1).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.Byte i] { get; }", derived.FindImplementationForInterfaceMember(p2).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.Int16 i] { get; }", derived.FindImplementationForInterfaceMember(p3).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.UInt16 i] { get; }", derived.FindImplementationForInterfaceMember(p4).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.Int32 i] { set; }", derived.FindImplementationForInterfaceMember(p5).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.UInt32 i] { set; }", derived.FindImplementationForInterfaceMember(p6).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.Int64 i] { get; set; }", derived.FindImplementationForInterfaceMember(p7).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.I1." + name + "[System.UInt64 i] { get; set; }", derived.FindImplementationForInterfaceMember(p8).ToTestDisplayString());

                if (m is PEModuleSymbol)
                {
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.SByte i)", derived.FindImplementationForInterfaceMember(p1.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.Byte i)", derived.FindImplementationForInterfaceMember(p2.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.Int16 i)", derived.FindImplementationForInterfaceMember(p3.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.UInt16 i)", derived.FindImplementationForInterfaceMember(p4.GetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.set_Item(System.Int32 i, System.Int32 value)", derived.FindImplementationForInterfaceMember(p5.SetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.set_Item(System.UInt32 i, System.Int32 value)", derived.FindImplementationForInterfaceMember(p6.SetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.Int64 i)", derived.FindImplementationForInterfaceMember(p7.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.get_Item(System.UInt64 i)", derived.FindImplementationForInterfaceMember(p8.GetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.set_Item(System.Int64 i, System.Int32 value)", derived.FindImplementationForInterfaceMember(p7.SetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.set_Item(System.UInt64 i, System.Int32 value)", derived.FindImplementationForInterfaceMember(p8.SetMethod).ToTestDisplayString());
                }
                else
                {
                    Assert.Equal("System.Int32 Test.I1.this[System.SByte i].get", derived.FindImplementationForInterfaceMember(p1.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.this[System.Byte i].get", derived.FindImplementationForInterfaceMember(p2.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.this[System.Int16 i].get", derived.FindImplementationForInterfaceMember(p3.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.this[System.UInt16 i].get", derived.FindImplementationForInterfaceMember(p4.GetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.this[System.Int32 i].set", derived.FindImplementationForInterfaceMember(p5.SetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.this[System.UInt32 i].set", derived.FindImplementationForInterfaceMember(p6.SetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.this[System.Int64 i].get", derived.FindImplementationForInterfaceMember(p7.GetMethod).ToTestDisplayString());
                    Assert.Equal("System.Int32 Test.I1.this[System.UInt64 i].get", derived.FindImplementationForInterfaceMember(p8.GetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.this[System.Int64 i].set", derived.FindImplementationForInterfaceMember(p7.SetMethod).ToTestDisplayString());
                    Assert.Equal("void Test.I1.this[System.UInt64 i].set", derived.FindImplementationForInterfaceMember(p8.SetMethod).ToTestDisplayString());
                }
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void IndexerImplementation_204()
        {
            var source1 =
@"
interface I1
{
    int this[sbyte i] => 1;
    int this[byte i] => 2;
    int this[short i] { get => 3; }
    int this[ushort i] { get => 4; }
    int this[int i] { set => System.Console.WriteLine(5); }
    int this[uint i] { set => System.Console.WriteLine(6); }
    int this[long i] { get { return 7;} set {} }
    int this[ulong i] { get { return 8;} set {} }
}

class Base : Test
{
    new int this[sbyte i] => 10;
    new int this[short i] { get => 30; }
    new int this[int i] { set => System.Console.WriteLine(50); }
    new int this[long i] { get { return 70;} set {} }
}

class Derived : Base, I1
{
    new int this[byte i] => 20;
    new int this[ushort i] { get => 40; }
    new int this[uint i] { set => System.Console.WriteLine(60); }
    new int this[ulong i] { get { return 80;} set {} }
}

class Test : I1 
{
    public int this[sbyte i] => 100;
    public int this[byte i] => 200;
    public int this[short i] { get => 300; }
    public int this[ushort i] { get => 400; }
    public int this[int i] { set => System.Console.WriteLine(500); }
    public int this[uint i] { set => System.Console.WriteLine(600); }
    public int this[long i] { get { return 700;} set {} }
    public int this[ulong i] { get { return 800;} set {} }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate(ModuleSymbol m)
            {
                var i1 = m.GlobalNamespace.GetTypeMember("I1");
                var indexers = i1.GetMembers("this[]");
                var p1 = (PropertySymbol)indexers[0];
                var p2 = (PropertySymbol)indexers[1];
                var p3 = (PropertySymbol)indexers[2];
                var p4 = (PropertySymbol)indexers[3];
                var p5 = (PropertySymbol)indexers[4];
                var p6 = (PropertySymbol)indexers[5];
                var p7 = (PropertySymbol)indexers[6];
                var p8 = (PropertySymbol)indexers[7];

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("System.Int32 Test.this[System.SByte i] { get; }", derived.FindImplementationForInterfaceMember(p1).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Byte i] { get; }", derived.FindImplementationForInterfaceMember(p2).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Int16 i] { get; }", derived.FindImplementationForInterfaceMember(p3).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.UInt16 i] { get; }", derived.FindImplementationForInterfaceMember(p4).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Int32 i] { set; }", derived.FindImplementationForInterfaceMember(p5).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.UInt32 i] { set; }", derived.FindImplementationForInterfaceMember(p6).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Int64 i] { get; set; }", derived.FindImplementationForInterfaceMember(p7).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.UInt64 i] { get; set; }", derived.FindImplementationForInterfaceMember(p8).ToTestDisplayString());

                Assert.Equal("System.Int32 Test.this[System.SByte i].get", derived.FindImplementationForInterfaceMember(p1.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Byte i].get", derived.FindImplementationForInterfaceMember(p2.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Int16 i].get", derived.FindImplementationForInterfaceMember(p3.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.UInt16 i].get", derived.FindImplementationForInterfaceMember(p4.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.this[System.Int32 i].set", derived.FindImplementationForInterfaceMember(p5.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.this[System.UInt32 i].set", derived.FindImplementationForInterfaceMember(p6.SetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.Int64 i].get", derived.FindImplementationForInterfaceMember(p7.GetMethod).ToTestDisplayString());
                Assert.Equal("System.Int32 Test.this[System.UInt64 i].get", derived.FindImplementationForInterfaceMember(p8.GetMethod).ToTestDisplayString());
                Assert.Equal("void Test.this[System.Int64 i].set", derived.FindImplementationForInterfaceMember(p7.SetMethod).ToTestDisplayString());
                Assert.Equal("void Test.this[System.UInt64 i].set", derived.FindImplementationForInterfaceMember(p8.SetMethod).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void IndexerImplementation_501()
        {
            var source1 =
@"
public interface I1
{
    int this[sbyte i] => 1;
    int this[short i] 
    { get => 3; }
    int this[int i] 
    { set => System.Console.WriteLine(5); }
    int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}

class Test1 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;
            compilation1.VerifyDiagnostics(
                // (4,26): error CS8501: Target runtime doesn't support default interface implementation.
                //     int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "1").WithLocation(4, 26),
                // (6,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(6, 7),
                // (8,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(8, 7),
                // (11,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(11, 9),
                // (12,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         set {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(12, 9)
                );

            ValidateIndexerImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.this[long].set' cannot implement interface member 'I1.this[long].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].set", "I1.this[long].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[sbyte].get' cannot implement interface member 'I1.this[sbyte].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[sbyte].get", "I1.this[sbyte].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[short].get' cannot implement interface member 'I1.this[short].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[short].get", "I1.this[short].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[int].set' cannot implement interface member 'I1.this[int].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[int].set", "I1.this[int].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[long].get' cannot implement interface member 'I1.this[long].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].get", "I1.this[long].get", "Test2")
                );

            ValidateIndexerImplementation_501(compilation3.SourceModule, "Test2");
        }

        private static void ValidateIndexerImplementation_501(ModuleSymbol m, string typeName)
        {
            var derived = m.GlobalNamespace.GetTypeMember(typeName);
            var i1 = derived.Interfaces.Single();
            Assert.Equal("I1", i1.ToTestDisplayString());

            var indexers = i1.GetMembers("this[]");
            var p1 = (PropertySymbol)indexers[0];
            var p3 = (PropertySymbol)indexers[1];
            var p5 = (PropertySymbol)indexers[2];
            var p7 = (PropertySymbol)indexers[3];

            Assert.True(p1.IsVirtual);
            Assert.True(p3.IsVirtual);
            Assert.True(p5.IsVirtual);
            Assert.True(p7.IsVirtual);

            Assert.False(p1.IsAbstract);
            Assert.False(p3.IsAbstract);
            Assert.False(p5.IsAbstract);
            Assert.False(p7.IsAbstract);

            Assert.Same(p1, derived.FindImplementationForInterfaceMember(p1));
            Assert.Same(p3, derived.FindImplementationForInterfaceMember(p3));
            Assert.Same(p5, derived.FindImplementationForInterfaceMember(p5));
            Assert.Same(p7, derived.FindImplementationForInterfaceMember(p7));

            Assert.True(p1.GetMethod.IsVirtual);
            Assert.True(p3.GetMethod.IsVirtual);
            Assert.True(p5.SetMethod.IsVirtual);
            Assert.True(p7.GetMethod.IsVirtual);
            Assert.True(p7.SetMethod.IsVirtual);

            Assert.True(p1.GetMethod.IsMetadataVirtual());
            Assert.True(p3.GetMethod.IsMetadataVirtual());
            Assert.True(p5.SetMethod.IsMetadataVirtual());
            Assert.True(p7.GetMethod.IsMetadataVirtual());
            Assert.True(p7.SetMethod.IsMetadataVirtual());

            Assert.False(p1.GetMethod.IsAbstract);
            Assert.False(p3.GetMethod.IsAbstract);
            Assert.False(p5.SetMethod.IsAbstract);
            Assert.False(p7.GetMethod.IsAbstract);
            Assert.False(p7.SetMethod.IsAbstract);

            Assert.Same(p1.GetMethod, derived.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Same(p3.GetMethod, derived.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Same(p5.SetMethod, derived.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Same(p7.GetMethod, derived.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Same(p7.SetMethod, derived.FindImplementationForInterfaceMember(p7.SetMethod));
        }

        [Fact]
        public void IndexerImplementation_502()
        {
            var source1 =
@"
public interface I1
{
    int this[sbyte i] => 1;
    int this[short i] 
    { get => 3; }
    int this[int i] 
    { set => System.Console.WriteLine(5); }
    int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}
";

            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
class Test2 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.this[short].get' cannot implement interface member 'I1.this[short].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[short].get", "I1.this[short].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[int].set' cannot implement interface member 'I1.this[int].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[int].set", "I1.this[int].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[long].get' cannot implement interface member 'I1.this[long].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].get", "I1.this[long].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[long].set' cannot implement interface member 'I1.this[long].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].set", "I1.this[long].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[sbyte].get' cannot implement interface member 'I1.this[sbyte].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[sbyte].get", "I1.this[sbyte].get", "Test2")
                );

            ValidateIndexerImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void IndexerImplementation_503()
        {
            var source1 =
@"
public interface I1
{
    int this[sbyte i] => 1;
    int this[short i] 
    { get => 3; }
    int this[int i] 
    { set => System.Console.WriteLine(5); }
    int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}
";

            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
public interface I2
{
    void M2();
}

class Test2 : I2
{
    public void M2() {}
}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            var test2 = compilation3.GetTypeByMetadataName("Test2");
            var i1 = compilation3.GetTypeByMetadataName("I1");
            Assert.Equal("I1", i1.ToTestDisplayString());

            var indexers = i1.GetMembers("this[]");
            var p1 = (PropertySymbol)indexers[0];
            var p3 = (PropertySymbol)indexers[1];
            var p5 = (PropertySymbol)indexers[2];
            var p7 = (PropertySymbol)indexers[3];

            Assert.Null(test2.FindImplementationForInterfaceMember(p1));
            Assert.Null(test2.FindImplementationForInterfaceMember(p3));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7));

            Assert.Null(test2.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p3.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5.SetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7.GetMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(p7.SetMethod));

            compilation3.VerifyDiagnostics();
        }

        [Fact]
        public void IndexerImplementation_601()
        {
            var source1 =
@"
public interface I1
{
    int this[sbyte i] => 1;
    int this[short i] 
    { get => 3; }
    int this[int i] 
    { set => System.Console.WriteLine(5); }
    int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}

class Test1 : I1
{}
";
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 26),
                // (4,26): error CS8501: Target runtime doesn't support default interface implementation.
                //     int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "1").WithLocation(4, 26),
                // (6,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(6, 7),
                // (6,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(6, 7),
                // (8,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(8, 7),
                // (8,7): error CS8501: Target runtime doesn't support default interface implementation.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(8, 7),
                // (11,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(11, 9),
                // (11,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "get").WithLocation(11, 9),
                // (12,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         set {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(12, 9),
                // (12,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         set {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "set").WithLocation(12, 9)
                );

            ValidateIndexerImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.this[long].set' cannot implement interface member 'I1.this[long].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].set", "I1.this[long].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[sbyte].get' cannot implement interface member 'I1.this[sbyte].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[sbyte].get", "I1.this[sbyte].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[short].get' cannot implement interface member 'I1.this[short].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[short].get", "I1.this[short].get", "Test2"),
                // (2,15): error CS8502: 'I1.this[int].set' cannot implement interface member 'I1.this[int].set' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[int].set", "I1.this[int].set", "Test2"),
                // (2,15): error CS8502: 'I1.this[long].get' cannot implement interface member 'I1.this[long].get' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.this[long].get", "I1.this[long].get", "Test2")
                );

            ValidateIndexerImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void IndexerImplementation_701()
        {
            var source1 =
@"
public interface I1
{
    int this[sbyte i] => 1;
    int this[short i] 
    { get => 3; }
    int this[int i] 
    { set => System.Console.WriteLine(5); }
    int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 26),
                // (6,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(6, 7),
                // (8,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(8, 7),
                // (11,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(11, 9),
                // (12,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         set {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(12, 9)
                );

            ValidateIndexerImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            ValidateIndexerImplementation_501(compilation2.SourceModule, "Test2");

            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                    ValidateIndexerImplementation_501(m, "Test2");
                });
        }

        [Fact]
        public void IndexerImplementation_901()
        {
            var source1 =
@"
public interface I1
{
    static int this[sbyte i] => 1;
    static int this[short i] 
    { get => 3; }
    static int this[int i] 
    { set => System.Console.WriteLine(5); }
    static int this[long i] 
    { 
        get { return 7;} 
        set {} 
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (4,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(4, 16),
                // (4,33): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     static int this[sbyte i] => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "7.1").WithLocation(4, 33),
                // (5,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int this[short i] 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(5, 16),
                // (6,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { get => 3; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(6, 7),
                // (7,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int this[int i] 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(7, 16),
                // (8,7): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //     { set => System.Console.WriteLine(5); }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(8, 7),
                // (9,16): error CS0106: The modifier 'static' is not valid for this item
                //     static int this[long i] 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(9, 16),
                // (11,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         get { return 7;} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "get").WithArguments("default interface implementation", "7.1").WithLocation(11, 9),
                // (12,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         set {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "set").WithArguments("default interface implementation", "7.1").WithLocation(12, 9)
                );

            ValidateIndexerImplementation_501(compilation1.SourceModule, "Test1");
        }

        [Fact]
        public void EventImplementation_101()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        add
        {
            System.Console.WriteLine(""add E1"");
        }
    }
}

class Test1 : I1
{}
";
            ValidateEventImplementation_101(source1, 
                new[] {
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 25)
                },
                haveAdd: true, haveRemove: false);
        }

        private void ValidateEventImplementation_101(string source1, DiagnosticDescription[] expected, bool haveAdd, bool haveRemove)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(expected);

            ValidateEventImplementationTest1_101(compilation1.SourceModule, haveAdd, haveRemove);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd, haveRemove);
            }

            Validate2(compilation2.SourceModule);
            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);
        }

        private static void ValidateEventImplementationTest1_101(ModuleSymbol m, bool haveAdd, bool haveRemove)
        {
            var i1 = m.GlobalNamespace.GetTypeMember("I1");
            var e1 = i1.GetMember<EventSymbol>("E1");
            var addE1 = e1.AddMethod;
            var rmvE1 = e1.RemoveMethod;

            if (haveAdd)
            {
                ValidateAccessor(addE1);
            }
            else
            {
                Assert.Null(addE1);
            }

            if (haveRemove)
            {
                ValidateAccessor(rmvE1);
            }
            else
            {
                Assert.Null(rmvE1);
            }

            void ValidateAccessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.True(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            Assert.False(e1.IsAbstract);
            Assert.True(e1.IsVirtual);
            Assert.False(e1.IsSealed);
            Assert.False(e1.IsStatic);
            Assert.False(e1.IsExtern);
            Assert.False(e1.IsOverride);
            Assert.Equal(Accessibility.Public, e1.DeclaredAccessibility);

            Assert.True(i1.IsAbstract);
            Assert.True(i1.IsMetadataAbstract);

            if (m is PEModuleSymbol peModule)
            {
                int rva;

                if (haveAdd)
                {
                    peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)addE1).Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);
                }

                if (haveRemove)
                {
                    peModule.Module.GetMethodDefPropsOrThrow(((PEMethodSymbol)rmvE1).Handle, out _, out _, out _, out rva);
                    Assert.NotEqual(0, rva);
                }
            }

            var test1 = m.GlobalNamespace.GetTypeMember("Test1");
            Assert.Equal("I1", test1.Interfaces.Single().ToTestDisplayString());
            Assert.Same(e1, test1.FindImplementationForInterfaceMember(e1));

            if (haveAdd)
            {
                Assert.Same(addE1, test1.FindImplementationForInterfaceMember(addE1));
            }

            if (haveRemove)
            {
                Assert.Same(rmvE1, test1.FindImplementationForInterfaceMember(rmvE1));
            }
        }

        private static void ValidateEventImplementationTest2_101(ModuleSymbol m, bool haveAdd, bool haveRemove)
        {
            var test2 = m.GlobalNamespace.GetTypeMember("Test2");
            Assert.Equal("I1", test2.Interfaces.Single().ToTestDisplayString());

            var e1 = test2.Interfaces.Single().GetMember<EventSymbol>("E1");
            Assert.Same(e1, test2.FindImplementationForInterfaceMember(e1));

            if (haveAdd)
            {
                var addP1 = e1.AddMethod;
                Assert.Same(addP1, test2.FindImplementationForInterfaceMember(addP1));
            }

            if (haveRemove)
            {
                var rmvP1 = e1.RemoveMethod;
                Assert.Same(rmvP1, test2.FindImplementationForInterfaceMember(rmvP1));
            }
        }

        [Fact]
        public void EventImplementation_102()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        add => System.Console.WriteLine(""add E1"");
        remove => System.Console.WriteLine(""remove E1"");
    }
}

class Test1 : I1
{}
";
            ValidateEventImplementation_102(source1);
        }

        private void ValidateEventImplementation_102(string source1)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            void Validate1(ModuleSymbol m)
            {
                ValidateEventImplementationTest1_101(m, haveAdd: true, haveRemove: true);
            }

            Validate1(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate2(ModuleSymbol m)
            {
                ValidateEventImplementationTest2_101(m, haveAdd: true, haveRemove: true);
            }

            Validate2(compilation2.SourceModule);

            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, verify: false, symbolValidator: Validate2);

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation1.EmitToImageReference() }, options: TestOptions.DebugDll);
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            Validate2(compilation3.SourceModule);

            compilation3.VerifyDiagnostics();
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate2);
        }

        [Fact]
        public void EventImplementation_103()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        remove
        {
            System.Console.WriteLine(""remove E1"");
        }
    }
}

class Test1 : I1
{}
";

            ValidateEventImplementation_101(source1,
                new[] {
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 25)
                },
                haveAdd: false, haveRemove: true);
        }

        [Fact]
        public void EventImplementation_104()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1
    {
        add;
    }
}

class Test1 : I1
{}
";

            ValidateEventImplementation_101(source1,
                new[] {
                // (6,12): error CS0073: An add or remove accessor must have a body
                //         add;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 12),
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 25)
                },
                haveAdd: true, haveRemove: false);
        }

        [Fact]
        public void EventImplementation_105()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        remove;
    }
}

class Test1 : I1
{}
";

            ValidateEventImplementation_101(source1,
                new[] {
                // (6,15): error CS0073: An add or remove accessor must have a body
                //         remove;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 15),
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 25)
                },
                haveAdd: false, haveRemove: true);
        }

        [Fact]
        public void EventImplementation_106()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
    }
}

class Test1 : I1
{}
";
            ValidateEventImplementation_101(source1,
                new[] {
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 25)
                },
                haveAdd: false, haveRemove: false);
        }

        [Fact]
        public void EventImplementation_107()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        add;
        remove;
    }
}

class Test1 : I1
{}
";
            ValidateEventImplementation_101(source1,
                new[] {
                // (6,12): error CS0073: An add or remove accessor must have a body
                //         add;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 12),
                // (7,15): error CS0073: An add or remove accessor must have a body
                //         remove;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(7, 15)
                },
                haveAdd: true, haveRemove: true);
        }

        [Fact]
        public void EventImplementation_108()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        get;
        set;
    } => 0;
}

class Test1 : I1
{}
";
            ValidateEventImplementation_101(source1,
                new[] {
                // (8,7): error CS1519: Invalid token '=>' in class, struct, or interface member declaration
                //     } => 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=>").WithArguments("=>").WithLocation(8, 7),
                // (6,9): error CS1055: An add or remove accessor expected
                //         get;
                Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "get").WithLocation(6, 9),
                // (7,9): error CS1055: An add or remove accessor expected
                //         set;
                Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "set").WithLocation(7, 9),
                // (4,25): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1")
                },
                haveAdd: false, haveRemove: false);
        }

        [Fact]
        public void EventImplementation_109()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        add => throw null;
        remove;
    }
}

class Test1 : I1
{}
";
            ValidateEventImplementation_101(source1,
                new[] {
                // (7,15): error CS0073: An add or remove accessor must have a body
                //         remove;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(7, 15)
                },
                haveAdd: true, haveRemove: true);
        }

        [Fact]
        public void EventImplementation_110()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E1 
    {
        add;
        remove => throw null;
    }
}

class Test1 : I1
{}
";
            ValidateEventImplementation_101(source1,
                new[] {
                // (6,12): error CS0073: An add or remove accessor must have a body
                //         add;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 12)
                },
                haveAdd: true, haveRemove: true);
        }

        [Fact]
        public void EventImplementation_201()
        {
            var source1 =
@"
interface I1
{
    event System.Action E7 { add {} remove {} }
    event System.Action E8 { add {} remove {} }
}

class Base
{
    event System.Action E7;
}

class Derived : Base, I1
{
    event System.Action E8 { add {} remove {} }
}

class Test : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (10,25): warning CS0067: The event 'Base.E7' is never used
                //     event System.Action E7;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E7").WithArguments("Base.E7").WithLocation(10, 25)
                );

            ValidateEventImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidateEventImplementation_201(m);
                });
        }

        private static void ValidateEventImplementation_201(ModuleSymbol m)
        {
            var e7 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E7");
            var e8 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E8");

            var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

            Assert.Same(e7, derived.FindImplementationForInterfaceMember(e7));
            Assert.Same(e8, derived.FindImplementationForInterfaceMember(e8));

            Assert.Same(e7.AddMethod, derived.FindImplementationForInterfaceMember(e7.AddMethod));
            Assert.Same(e8.AddMethod, derived.FindImplementationForInterfaceMember(e8.AddMethod));
            Assert.Same(e7.RemoveMethod, derived.FindImplementationForInterfaceMember(e7.RemoveMethod));
            Assert.Same(e8.RemoveMethod, derived.FindImplementationForInterfaceMember(e8.RemoveMethod));
        }

        [Fact]
        public void EventImplementation_202()
        {
            var source1 =
@"
interface I1
{
    event System.Action E7 { add {} remove {} }
    event System.Action E8 { add {} remove {} }
}

class Base : Test
{
    event System.Action E7;
}

class Derived : Base, I1
{
    event System.Action E8 { add {} remove {} }
}

class Test : I1 {}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (10,25): warning CS0067: The event 'Base.E7' is never used
                //     event System.Action E7;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E7").WithArguments("Base.E7").WithLocation(10, 25)
                );

            ValidateEventImplementation_201(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    ValidateEventImplementation_201(m);
                });
        }

        [Fact]
        public void EventImplementation_203()
        {
            var source1 =
@"
interface I1
{
    event System.Action E7 { add {} remove {} }
    event System.Action E8 { add {} remove {} }
}

class Base : Test
{
    event System.Action E7;
}

class Derived : Base, I1
{
    event System.Action E8 { add {} remove {} }
}

class Test : I1 
{
    event System.Action I1.E7 { add {} remove {} }
    event System.Action I1.E8 { add {} remove {} }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (10,25): warning CS0067: The event 'Base.E7' is never used
                //     event System.Action E7;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E7").WithArguments("Base.E7").WithLocation(10, 25)
                );

            void Validate(ModuleSymbol m)
            {
                var e7 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E7");
                var e8 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E8");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("event System.Action Test.I1.E7", derived.FindImplementationForInterfaceMember(e7).ToTestDisplayString());
                Assert.Equal("event System.Action Test.I1.E8", derived.FindImplementationForInterfaceMember(e8).ToTestDisplayString());

                Assert.Equal("void Test.I1.E7.add", derived.FindImplementationForInterfaceMember(e7.AddMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.E8.add", derived.FindImplementationForInterfaceMember(e8.AddMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.E7.remove", derived.FindImplementationForInterfaceMember(e7.RemoveMethod).ToTestDisplayString());
                Assert.Equal("void Test.I1.E8.remove", derived.FindImplementationForInterfaceMember(e8.RemoveMethod).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void EventImplementation_204()
        {
            var source1 =
@"
interface I1
{
    event System.Action E7 { add {} remove {} }
    event System.Action E8 { add {} remove {} }
}

class Base : Test
{
    new event System.Action E7;
}

class Derived : Base, I1
{
    new event System.Action E8 { add {} remove {} }
}

class Test : I1 
{
    public event System.Action E7 { add {} remove {} }
    public event System.Action E8 { add {} remove {} }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (10,29): warning CS0067: The event 'Base.E7' is never used
                //     new event System.Action E7;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E7").WithArguments("Base.E7").WithLocation(10, 29)
                );

            void Validate(ModuleSymbol m)
            {
                var e7 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E7");
                var e8 = m.GlobalNamespace.GetMember<EventSymbol>("I1.E8");

                var derived = m.ContainingAssembly.GetTypeByMetadataName("Derived");

                Assert.Equal("event System.Action Test.E7", derived.FindImplementationForInterfaceMember(e7).ToTestDisplayString());
                Assert.Equal("event System.Action Test.E8", derived.FindImplementationForInterfaceMember(e8).ToTestDisplayString());

                Assert.Equal("void Test.E7.add", derived.FindImplementationForInterfaceMember(e7.AddMethod).ToTestDisplayString());
                Assert.Equal("void Test.E8.add", derived.FindImplementationForInterfaceMember(e8.AddMethod).ToTestDisplayString());
                Assert.Equal("void Test.E7.remove", derived.FindImplementationForInterfaceMember(e7.RemoveMethod).ToTestDisplayString());
                Assert.Equal("void Test.E8.remove", derived.FindImplementationForInterfaceMember(e8.RemoveMethod).ToTestDisplayString());
            }

            Validate(compilation1.SourceModule);

            CompileAndVerify(compilation1, verify: false,
                symbolValidator: (m) =>
                {
                    var derivedResult = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Derived");
                    Assert.Equal("I1", derivedResult.Interfaces.Single().ToTestDisplayString());

                    Validate(m);
                });
        }

        [Fact]
        public void EventImplementation_501()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}

class Test1 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;
            compilation1.VerifyDiagnostics(
                // (6,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         add {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "add").WithLocation(6, 9),
                // (7,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         remove {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "remove").WithLocation(7, 9)
                );

            ValidateEventImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.E7.remove' cannot implement interface member 'I1.E7.remove' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.remove", "I1.E7.remove", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.E7.add' cannot implement interface member 'I1.E7.add' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.add", "I1.E7.add", "Test2").WithLocation(2, 15)
                );

            ValidateEventImplementation_501(compilation3.SourceModule, "Test2");
        }

        private static void ValidateEventImplementation_501(ModuleSymbol m, string typeName)
        {
            var derived = m.GlobalNamespace.GetTypeMember(typeName);
            var i1 = derived.Interfaces.Single();
            Assert.Equal("I1", i1.ToTestDisplayString());

            var e7 = i1.GetMember<EventSymbol>("E7");

            Assert.True(e7.IsVirtual);
            Assert.False(e7.IsAbstract);

            Assert.Same(e7, derived.FindImplementationForInterfaceMember(e7));

            Assert.True(e7.AddMethod.IsVirtual);
            Assert.True(e7.RemoveMethod.IsVirtual);

            Assert.True(e7.AddMethod.IsMetadataVirtual());
            Assert.True(e7.RemoveMethod.IsMetadataVirtual());

            Assert.False(e7.AddMethod.IsAbstract);
            Assert.False(e7.RemoveMethod.IsAbstract);

            Assert.Same(e7.AddMethod, derived.FindImplementationForInterfaceMember(e7.AddMethod));
            Assert.Same(e7.RemoveMethod, derived.FindImplementationForInterfaceMember(e7.RemoveMethod));
        }

        [Fact]
        public void EventImplementation_502()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}
";

            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
class Test2 : I1
{}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.E7.remove' cannot implement interface member 'I1.E7.remove' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.remove", "I1.E7.remove", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.E7.add' cannot implement interface member 'I1.E7.add' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.add", "I1.E7.add", "Test2").WithLocation(2, 15)
                );

            ValidateEventImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void EventImplementation_503()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}
";

            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics();

            var source2 =
@"
public interface I2
{
    void M2();
}

class Test2 : I2
{
    public void M2() {}
}
";

            // Avoid sharing mscorlib symbols with other tests since we are about to change
            // RuntimeSupportsDefaultInterfaceImplementation property for it.
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            var test2 = compilation3.GetTypeByMetadataName("Test2");
            var i1 = compilation3.GetTypeByMetadataName("I1");
            Assert.Equal("I1", i1.ToTestDisplayString());

            var e7 = i1.GetMember<EventSymbol>("E7");

            Assert.Null(test2.FindImplementationForInterfaceMember(e7));

            Assert.Null(test2.FindImplementationForInterfaceMember(e7.AddMethod));
            Assert.Null(test2.FindImplementationForInterfaceMember(e7.RemoveMethod));

            compilation3.VerifyDiagnostics();
        }

        [Fact]
        public void EventImplementation_601()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}

class Test1 : I1
{}
";
            var mscorLibRef = MscorlibRefWithoutSharingCachedSymbols;
            var compilation1 = CreateCompilation(source1, new[] { mscorLibRef }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation = false;

            compilation1.VerifyDiagnostics(
                // (6,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         add {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(6, 9),
                // (6,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         add {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "add").WithLocation(6, 9),
                // (7,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         remove {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(7, 9),
                // (7,9): error CS8501: Target runtime doesn't support default interface implementation.
                //         remove {} 
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "remove").WithLocation(7, 9)
                );

            ValidateEventImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation3 = CreateCompilation(source2, new[] { mscorLibRef, compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.False(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation3.VerifyDiagnostics(
                // (2,15): error CS8502: 'I1.E7.remove' cannot implement interface member 'I1.E7.remove' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.remove", "I1.E7.remove", "Test2").WithLocation(2, 15),
                // (2,15): error CS8502: 'I1.E7.add' cannot implement interface member 'I1.E7.add' in type 'Test2' because the target runtime doesn't support default interface implementation.
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember, "I1").WithArguments("I1.E7.add", "I1.E7.add", "Test2").WithLocation(2, 15)
                );

            ValidateEventImplementation_501(compilation3.SourceModule, "Test2");
        }

        [Fact]
        public void EventImplementation_701()
        {
            var source1 =
@"
public interface I1
{
    event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (6,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         add {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(6, 9),
                // (7,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         remove {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(7, 9)
                );

            ValidateEventImplementation_501(compilation1.SourceModule, "Test1");

            var source2 =
@"
class Test2 : I1
{}
";

            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                            parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            ValidateEventImplementation_501(compilation2.SourceModule, "Test2");

            CompileAndVerify(compilation2, verify: false,
                symbolValidator: (m) =>
                {
                    var test2Result = (PENamedTypeSymbol)m.GlobalNamespace.GetTypeMember("Test2");
                    Assert.Equal("I1", test2Result.Interfaces.Single().ToTestDisplayString());
                    ValidateEventImplementation_501(m, "Test2");
                });
        }

        [Fact]
        public void EventImplementation_901()
        {
            var source1 =
@"
public interface I1
{
    static event System.Action E7 
    { 
        add {} 
        remove {} 
    }
}

class Test1 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (6,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         add {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(6, 9),
                // (7,9): error CS8107: Feature 'default interface implementation' is not available in C# 7.  Please use language version 7.1 or greater.
                //         remove {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(7, 9)
                );

            var derived = compilation1.GlobalNamespace.GetTypeMember("Test1");
            var i1 = derived.Interfaces.Single();
            Assert.Equal("I1", i1.ToTestDisplayString());

            var e7 = i1.GetMember<EventSymbol>("E7");

            Assert.False(e7.IsVirtual);
            Assert.False(e7.IsAbstract);
            Assert.True(e7.IsStatic);

            Assert.Null(derived.FindImplementationForInterfaceMember(e7));

            Assert.False(e7.AddMethod.IsVirtual);
            Assert.False(e7.RemoveMethod.IsVirtual);

            Assert.False(e7.AddMethod.IsMetadataVirtual());
            Assert.False(e7.RemoveMethod.IsMetadataVirtual());

            Assert.False(e7.AddMethod.IsAbstract);
            Assert.False(e7.RemoveMethod.IsAbstract);

            Assert.True(e7.AddMethod.IsStatic);
            Assert.True(e7.RemoveMethod.IsStatic);

            Assert.Null(derived.FindImplementationForInterfaceMember(e7.AddMethod));
            Assert.Null(derived.FindImplementationForInterfaceMember(e7.RemoveMethod));
        }

        [Fact]
        public void BaseIsNotAllowed_01()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        base.GetHashCode();
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (6,9): error CS0174: A base class is required for a 'base' reference
                //         base.GetHashCode();
                Diagnostic(ErrorCode.ERR_NoBaseClass, "base").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ThisIsAllowed_01()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""I1.M1"");
    }

    int P1
    {
        get
        {
            System.Console.WriteLine(""I1.get_P1"");
            return 0;
        }
        set => System.Console.WriteLine(""I1.set_P1"");
    }

    event System.Action E1
    {
        add => System.Console.WriteLine(""I1.add_E1"");
        remove => System.Console.WriteLine(""I1.remove_E1"");
    }
}

public interface I2 : I1
{
    void M2() 
    {
        System.Console.WriteLine(""I2.M2"");
        System.Console.WriteLine(this.GetHashCode());
        this.M1();
        this.P1 = this.P1;
        this.E1 += null;
        this.E1 -= null;
        this.M3();
        this.P3 = this.P3;
        this.E3 += null;
        this.E3 -= null;
    }

    int P2
    {
        get
        {
            System.Console.WriteLine(""I2.get_P2"");
            System.Console.WriteLine(this.GetHashCode());
            this.M1();
            this.P1 = this.P1;
            this.E1 += null;
            this.E1 -= null;
            this.M3();
            this.P3 = this.P3;
            this.E3 += null;
            this.E3 -= null;
            return 0;
        }
        set
        {
            System.Console.WriteLine(""I2.set_P2"");
            System.Console.WriteLine(this.GetHashCode());
            this.M1();
            this.P1 = this.P1;
            this.E1 += null;
            this.E1 -= null;
            this.M3();
            this.P3 = this.P3;
            this.E3 += null;
            this.E3 -= null;
        }
    }

    event System.Action E2
    {
        add
        {
            System.Console.WriteLine(""I2.add_E2"");
            System.Console.WriteLine(this.GetHashCode());
            this.M1();
            this.P1 = this.P1;
            this.E1 += null;
            this.E1 -= null;
            this.M3();
            this.P3 = this.P3;
            this.E3 += null;
            this.E3 -= null;
        }
        remove
        {
            System.Console.WriteLine(""I2.remove_E2"");
            System.Console.WriteLine(this.GetHashCode());
            this.M1();
            this.P1 = this.P1;
            this.E1 += null;
            this.E1 -= null;
            this.M3();
            this.P3 = this.P3;
            this.E3 += null;
            this.E3 -= null;
        }
    }

    void M3() 
    {
        System.Console.WriteLine(""I2.M3"");
    }

    int P3
    {
        get
        {
            System.Console.WriteLine(""I2.get_P3"");
            return 0;
        }
        set => System.Console.WriteLine(""I2.set_P3"");
    }

    event System.Action E3
    {
        add => System.Console.WriteLine(""I2.add_E3"");
        remove => System.Console.WriteLine(""I2.remove_E3"");
    }
}


class Test1 : I2
{
    static void Main()
    {
        I2 x = new Test1();
        x.M2();
        x.P2 = x.P2;
        x.E2 += null;
        x.E2 -= null;
    }

    public override int GetHashCode()
    {
        return 123;
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            CompileAndVerify(compilation1, verify: false);

/* Expected output
I2.M2
123
I1.M1
I1.get_P1
I1.set_P1
I1.add_E1
I1.remove_E1
I2.M3
I2.get_P3
I2.set_P3
I2.add_E3
I2.remove_E3
I2.get_P2
123
I1.M1
I1.get_P1
I1.set_P1
I1.add_E1
I1.remove_E1
I2.M3
I2.get_P3
I2.set_P3
I2.add_E3
I2.remove_E3
I2.set_P2
123
I1.M1
I1.get_P1
I1.set_P1
I1.add_E1
I1.remove_E1
I2.M3
I2.get_P3
I2.set_P3
I2.add_E3
I2.remove_E3
I2.add_E2
123
I1.M1
I1.get_P1
I1.set_P1
I1.add_E1
I1.remove_E1
I2.M3
I2.get_P3
I2.set_P3
I2.add_E3
I2.remove_E3
I2.remove_E2
123
I1.M1
I1.get_P1
I1.set_P1
I1.add_E1
I1.remove_E1
I2.M3
I2.get_P3
I2.set_P3
I2.add_E3
I2.remove_E3
*/
        }

        [Fact]
        public void ThisIsAllowed_02()
        {
            var source1 =
@"
public interface I1
{
    public int F1;
}

public interface I2 : I1
{
    void M2() 
    {
        this.F1 = this.F2;
    }

    int P2
    {
        get
        {
            this.F1 = this.F2;
            return 0;
        }
        set
        {
            this.F1 = this.F2;
        }
    }

    event System.Action E2
    {
        add
        {
            this.F1 = this.F2;
        }
        remove
        {
            this.F1 = this.F2;
        }
    }

    public int F2;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,16): error CS0525: Interfaces cannot contain fields
                //     public int F1;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "F1").WithLocation(4, 16),
                // (39,16): error CS0525: Interfaces cannot contain fields
                //     public int F2;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "F2").WithLocation(39, 16)
                );
        }

        [Fact]
        public void ImplicitThisIsAllowed_01()
        {
            var source1 =
@"
public interface I1
{
    void M1() 
    {
        System.Console.WriteLine(""I1.M1"");
    }

    int P1
    {
        get
        {
            System.Console.WriteLine(""I1.get_P1"");
            return 0;
        }
        set => System.Console.WriteLine(""I1.set_P1"");
    }

    event System.Action E1
    {
        add => System.Console.WriteLine(""I1.add_E1"");
        remove => System.Console.WriteLine(""I1.remove_E1"");
    }
}

public interface I2 : I1
{
    void M2() 
    {
        System.Console.WriteLine(""I2.M2"");
        System.Console.WriteLine(GetHashCode());
        M1();
        P1 = P1;
        E1 += null;
        E1 -= null;
        M3();
        P3 = P3;
        E3 += null;
        E3 -= null;
    }

    int P2
    {
        get
        {
            System.Console.WriteLine(""I2.get_P2"");
            System.Console.WriteLine(GetHashCode());
            M1();
            P1 = P1;
            E1 += null;
            E1 -= null;
            M3();
            P3 = P3;
            E3 += null;
            E3 -= null;
            return 0;
        }
        set
        {
            System.Console.WriteLine(""I2.set_P2"");
            System.Console.WriteLine(GetHashCode());
            M1();
            P1 = P1;
            E1 += null;
            E1 -= null;
            M3();
            P3 = P3;
            E3 += null;
            E3 -= null;
        }
    }

    event System.Action E2
    {
        add
        {
            System.Console.WriteLine(""I2.add_E2"");
            System.Console.WriteLine(GetHashCode());
            M1();
            P1 = P1;
            E1 += null;
            E1 -= null;
            M3();
            P3 = P3;
            E3 += null;
            E3 -= null;
        }
        remove
        {
            System.Console.WriteLine(""I2.remove_E2"");
            System.Console.WriteLine(GetHashCode());
            M1();
            P1 = P1;
            E1 += null;
            E1 -= null;
            M3();
            P3 = P3;
            E3 += null;
            E3 -= null;
        }
    }

    void M3() 
    {
        System.Console.WriteLine(""I2.M3"");
    }

    int P3
    {
        get
        {
            System.Console.WriteLine(""I2.get_P3"");
            return 0;
        }
        set => System.Console.WriteLine(""I2.set_P3"");
    }

    event System.Action E3
    {
        add => System.Console.WriteLine(""I2.add_E3"");
        remove => System.Console.WriteLine(""I2.remove_E3"");
    }
}


class Test1 : I2
{
    static void Main()
    {
        I2 x = new Test1();
        x.M2();
        x.P2 = x.P2;
        x.E2 += null;
        x.E2 -= null;
    }

    public override int GetHashCode()
    {
        return 123;
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics();

            CompileAndVerify(compilation1, verify: false);

/* Expected output
I2.M2
123
I1.M1
I1.get_P1
I1.set_P1
I1.add_E1
I1.remove_E1
I2.M3
I2.get_P3
I2.set_P3
I2.add_E3
I2.remove_E3
I2.get_P2
123
I1.M1
I1.get_P1
I1.set_P1
I1.add_E1
I1.remove_E1
I2.M3
I2.get_P3
I2.set_P3
I2.add_E3
I2.remove_E3
I2.set_P2
123
I1.M1
I1.get_P1
I1.set_P1
I1.add_E1
I1.remove_E1
I2.M3
I2.get_P3
I2.set_P3
I2.add_E3
I2.remove_E3
I2.add_E2
123
I1.M1
I1.get_P1
I1.set_P1
I1.add_E1
I1.remove_E1
I2.M3
I2.get_P3
I2.set_P3
I2.add_E3
I2.remove_E3
I2.remove_E2
123
I1.M1
I1.get_P1
I1.set_P1
I1.add_E1
I1.remove_E1
I2.M3
I2.get_P3
I2.set_P3
I2.add_E3
I2.remove_E3
*/
        }

        [Fact]
        public void ImplicitThisIsAllowed_02()
        {
            var source1 =
@"
public interface I1
{
    public int F1;
}

public interface I2 : I1
{
    void M2() 
    {
        F1 = F2;
    }

    int P2
    {
        get
        {
            F1 = F2;
            return 0;
        }
        set
        {
            F1 = F2;
        }
    }

    event System.Action E2
    {
        add
        {
            F1 = F2;
        }
        remove
        {
            F1 = F2;
        }
    }

    public int F2;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,16): error CS0525: Interfaces cannot contain fields
                //     public int F1;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "F1").WithLocation(4, 16),
                // (39,16): error CS0525: Interfaces cannot contain fields
                //     public int F2;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "F2").WithLocation(39, 16)
                );
        }

        [Fact]
        public void MethodModifiers_01()
        {
            var source1 =
@"
public interface I1
{
    public void M01();
    protected void M02();
    protected internal void M03();
    internal void M04();
    private void M05();
    static void M06();
    virtual void M07();
    sealed void M08();
    override void M09();
    abstract void M10();
    extern void M11();
    async void M12();
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (5,20): error CS0106: The modifier 'protected' is not valid for this item
                //     protected void M02();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M02").WithArguments("protected").WithLocation(5, 20),
                // (6,29): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal void M03();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M03").WithArguments("protected internal").WithLocation(6, 29),
                // (12,19): error CS0106: The modifier 'override' is not valid for this item
                //     override void M09();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(12, 19),
                // (15,16): error CS1994: The 'async' modifier can only be used in methods that have a body.
                //     async void M12();
                Diagnostic(ErrorCode.ERR_BadAsyncLacksBody, "M12").WithLocation(15, 16),
                // (8,18): error CS0501: 'I1.M05()' must declare a body because it is not marked abstract, extern, or partial
                //     private void M05();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M05").WithArguments("I1.M05()").WithLocation(8, 18),
                // (9,17): error CS0501: 'I1.M06()' must declare a body because it is not marked abstract, extern, or partial
                //     static void M06();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M06").WithArguments("I1.M06()").WithLocation(9, 17),
                // (10,18): error CS0501: 'I1.M07()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual void M07();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M07").WithArguments("I1.M07()").WithLocation(10, 18),
                // (11,17): error CS0501: 'I1.M08()' must declare a body because it is not marked abstract, extern, or partial
                //     sealed void M08();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M08").WithArguments("I1.M08()").WithLocation(11, 17),
                // (14,17): warning CS0626: Method, operator, or accessor 'I1.M11()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern void M11();
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "M11").WithArguments("I1.M11()").WithLocation(14, 17)
                );

            ValidateSymbolsMethodModifiers_01(compilation1);
        }

        private static void ValidateSymbolsMethodModifiers_01(CSharpCompilation compilation1)
        { 
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>("M01");

            Assert.True(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.True(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.False(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Equal(Accessibility.Public, m01.DeclaredAccessibility);

            var m02 = i1.GetMember<MethodSymbol>("M02");

            Assert.True(m02.IsAbstract);
            Assert.False(m02.IsVirtual);
            Assert.True(m02.IsMetadataVirtual());
            Assert.False(m02.IsSealed);
            Assert.False(m02.IsStatic);
            Assert.False(m02.IsExtern);
            Assert.False(m02.IsAsync);
            Assert.False(m02.IsOverride);
            Assert.Equal(Accessibility.Public, m02.DeclaredAccessibility);

            var m03 = i1.GetMember<MethodSymbol>("M03");

            Assert.True(m03.IsAbstract);
            Assert.False(m03.IsVirtual);
            Assert.True(m03.IsMetadataVirtual());
            Assert.False(m03.IsSealed);
            Assert.False(m03.IsStatic);
            Assert.False(m03.IsExtern);
            Assert.False(m03.IsAsync);
            Assert.False(m03.IsOverride);
            Assert.Equal(Accessibility.Public, m03.DeclaredAccessibility);

            var m04 = i1.GetMember<MethodSymbol>("M04");

            Assert.True(m04.IsAbstract);
            Assert.False(m04.IsVirtual);
            Assert.True(m04.IsMetadataVirtual());
            Assert.False(m04.IsSealed);
            Assert.False(m04.IsStatic);
            Assert.False(m04.IsExtern);
            Assert.False(m04.IsAsync);
            Assert.False(m04.IsOverride);
            Assert.Equal(Accessibility.Internal, m04.DeclaredAccessibility);

            var m05 = i1.GetMember<MethodSymbol>("M05");

            Assert.False(m05.IsAbstract);
            Assert.False(m05.IsVirtual);
            Assert.False(m05.IsMetadataVirtual());
            Assert.False(m05.IsSealed);
            Assert.False(m05.IsStatic);
            Assert.False(m05.IsExtern);
            Assert.False(m05.IsAsync);
            Assert.False(m05.IsOverride);
            Assert.Equal(Accessibility.Private, m05.DeclaredAccessibility);

            var m06 = i1.GetMember<MethodSymbol>("M06");

            Assert.False(m06.IsAbstract);
            Assert.False(m06.IsVirtual);
            Assert.False(m06.IsMetadataVirtual());
            Assert.False(m06.IsSealed);
            Assert.True(m06.IsStatic);
            Assert.False(m06.IsExtern);
            Assert.False(m06.IsAsync);
            Assert.False(m06.IsOverride);
            Assert.Equal(Accessibility.Public, m06.DeclaredAccessibility);

            var m07 = i1.GetMember<MethodSymbol>("M07");

            Assert.False(m07.IsAbstract);
            Assert.True(m07.IsVirtual);
            Assert.True(m07.IsMetadataVirtual());
            Assert.False(m07.IsSealed);
            Assert.False(m07.IsStatic);
            Assert.False(m07.IsExtern);
            Assert.False(m07.IsAsync);
            Assert.False(m07.IsOverride);
            Assert.Equal(Accessibility.Public, m07.DeclaredAccessibility);

            var m08 = i1.GetMember<MethodSymbol>("M08");

            Assert.False(m08.IsAbstract);
            Assert.False(m08.IsVirtual);
            Assert.False(m08.IsMetadataVirtual());
            Assert.False(m08.IsSealed);
            Assert.False(m08.IsStatic);
            Assert.False(m08.IsExtern);
            Assert.False(m08.IsAsync);
            Assert.False(m08.IsOverride);
            Assert.Equal(Accessibility.Public, m08.DeclaredAccessibility);

            var m09 = i1.GetMember<MethodSymbol>("M09");

            Assert.True(m09.IsAbstract);
            Assert.False(m09.IsVirtual);
            Assert.True(m09.IsMetadataVirtual());
            Assert.False(m09.IsSealed);
            Assert.False(m09.IsStatic);
            Assert.False(m09.IsExtern);
            Assert.False(m09.IsAsync);
            Assert.False(m09.IsOverride);
            Assert.Equal(Accessibility.Public, m09.DeclaredAccessibility);

            var m10 = i1.GetMember<MethodSymbol>("M10");

            Assert.True(m10.IsAbstract);
            Assert.False(m10.IsVirtual);
            Assert.True(m10.IsMetadataVirtual());
            Assert.False(m10.IsSealed);
            Assert.False(m10.IsStatic);
            Assert.False(m10.IsExtern);
            Assert.False(m10.IsAsync);
            Assert.False(m10.IsOverride);
            Assert.Equal(Accessibility.Public, m10.DeclaredAccessibility);

            var m11 = i1.GetMember<MethodSymbol>("M11");

            Assert.False(m11.IsAbstract);
            Assert.True(m11.IsVirtual);
            Assert.True(m11.IsMetadataVirtual());
            Assert.False(m11.IsSealed);
            Assert.False(m11.IsStatic);
            Assert.True(m11.IsExtern);
            Assert.False(m11.IsAsync);
            Assert.False(m11.IsOverride);
            Assert.Equal(Accessibility.Public, m11.DeclaredAccessibility);

            var m12 = i1.GetMember<MethodSymbol>("M12");

            Assert.True(m12.IsAbstract);
            Assert.False(m12.IsVirtual);
            Assert.True(m12.IsMetadataVirtual());
            Assert.False(m12.IsSealed);
            Assert.False(m12.IsStatic);
            Assert.False(m12.IsExtern);
            Assert.True(m12.IsAsync);
            Assert.False(m12.IsOverride);
            Assert.Equal(Accessibility.Public, m12.DeclaredAccessibility);
        }

        [Fact]
        public void MethodModifiers_02()
        {
            var source1 =
@"
public interface I1
{
    public void M01();
    protected void M02();
    protected internal void M03();
    internal void M04();
    private void M05();
    static void M06();
    virtual void M07();
    sealed void M08();
    override void M09();
    abstract void M10();
    extern void M11();
    async void M12();
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,17): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public void M01();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M01").WithArguments("public", "7", "7.1").WithLocation(4, 17),
                // (5,20): error CS0106: The modifier 'protected' is not valid for this item
                //     protected void M02();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M02").WithArguments("protected").WithLocation(5, 20),
                // (6,29): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal void M03();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M03").WithArguments("protected internal").WithLocation(6, 29),
                // (7,19): error CS8503: The modifier 'internal' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     internal void M04();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M04").WithArguments("internal", "7", "7.1").WithLocation(7, 19),
                // (8,18): error CS8503: The modifier 'private' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     private void M05();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M05").WithArguments("private", "7", "7.1").WithLocation(8, 18),
                // (9,17): error CS8503: The modifier 'static' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     static void M06();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M06").WithArguments("static", "7", "7.1").WithLocation(9, 17),
                // (10,18): error CS8503: The modifier 'virtual' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     virtual void M07();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M07").WithArguments("virtual", "7", "7.1").WithLocation(10, 18),
                // (11,17): error CS8503: The modifier 'sealed' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     sealed void M08();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M08").WithArguments("sealed", "7", "7.1").WithLocation(11, 17),
                // (12,19): error CS0106: The modifier 'override' is not valid for this item
                //     override void M09();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(12, 19),
                // (13,19): error CS8503: The modifier 'abstract' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     abstract void M10();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M10").WithArguments("abstract", "7", "7.1").WithLocation(13, 19),
                // (14,17): error CS8503: The modifier 'extern' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     extern void M11();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M11").WithArguments("extern", "7", "7.1").WithLocation(14, 17),
                // (15,16): error CS8503: The modifier 'async' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     async void M12();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M12").WithArguments("async", "7", "7.1").WithLocation(15, 16),
                // (15,16): error CS1994: The 'async' modifier can only be used in methods that have a body.
                //     async void M12();
                Diagnostic(ErrorCode.ERR_BadAsyncLacksBody, "M12").WithLocation(15, 16),
                // (8,18): error CS0501: 'I1.M05()' must declare a body because it is not marked abstract, extern, or partial
                //     private void M05();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M05").WithArguments("I1.M05()").WithLocation(8, 18),
                // (9,17): error CS0501: 'I1.M06()' must declare a body because it is not marked abstract, extern, or partial
                //     static void M06();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M06").WithArguments("I1.M06()").WithLocation(9, 17),
                // (10,18): error CS0501: 'I1.M07()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual void M07();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M07").WithArguments("I1.M07()").WithLocation(10, 18),
                // (11,17): error CS0501: 'I1.M08()' must declare a body because it is not marked abstract, extern, or partial
                //     sealed void M08();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M08").WithArguments("I1.M08()").WithLocation(11, 17),
                // (14,17): warning CS0626: Method, operator, or accessor 'I1.M11()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern void M11();
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "M11").WithArguments("I1.M11()").WithLocation(14, 17)
                );

            ValidateSymbolsMethodModifiers_01(compilation1);
        }

        [Fact]
        public void MethodModifiers_03()
        {
            var source1 =
@"
public interface I1
{
    public virtual void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{}
";
            ValidateMethodImplementation_011(source1);
        }

        [Fact]
        public void MethodModifiers_04()
        {
            var source1 =
@"
public interface I1
{
    public abstract void M1(); 
    void M2(); 
}

class Test1 : I1
{
    public void M1() 
    {
        System.Console.WriteLine(""M1"");
    }

    public void M2() 
    {
        System.Console.WriteLine(""M2"");
    }

    static void Main()
    {
        I1 x = new Test1();
        x.M1();
        x.M2();
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            CompileAndVerify(compilation1, expectedOutput:
@"M1
M2", symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = m.GlobalNamespace.GetTypeMember("I1");

                foreach (var methodName in new[] { "M1", "M2" })
                {
                    var m1 = i1.GetMember<MethodSymbol>(methodName);

                    Assert.True(m1.IsAbstract);
                    Assert.False(m1.IsVirtual);
                    Assert.True(m1.IsMetadataVirtual());
                    Assert.False(m1.IsSealed);
                    Assert.False(m1.IsStatic);
                    Assert.False(m1.IsExtern);
                    Assert.False(m1.IsAsync);
                    Assert.False(m1.IsOverride);
                    Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
                    Assert.Same(test1.GetMember(methodName), test1.FindImplementationForInterfaceMember(m1));
                }
            }
        }

        [Fact]
        public void MethodModifiers_05()
        {
            var source1 =
@"
public interface I1
{
    public abstract void M1();
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,26): error CS8503: The modifier 'abstract' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public abstract void M1();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M1").WithArguments("abstract", "7", "7.1").WithLocation(4, 26),
                // (4,26): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public abstract void M1();
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "M1").WithArguments("public", "7", "7.1").WithLocation(4, 26)
                );

            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m1 = i1.GetMember<MethodSymbol>("M1");

            Assert.True(m1.IsAbstract);
            Assert.False(m1.IsVirtual);
            Assert.True(m1.IsMetadataVirtual());
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsStatic);
            Assert.False(m1.IsExtern);
            Assert.False(m1.IsAsync);
            Assert.False(m1.IsOverride);
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
        }

        [Fact]
        public void MethodModifiers_06()
        {
            var source1 =
@"
public interface I1
{
    public static void M1() 
    {
        System.Console.WriteLine(""M1"");
    }

    internal static void M2() 
    {
        System.Console.WriteLine(""M2"");
        M3();
    }

    private static void M3() 
    {
        System.Console.WriteLine(""M3"");
    }
}

class Test1 : I1
{
    static void Main()
    {
        I1.M1();
        I1.M2();
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, expectedOutput:
@"M1
M2
M3", symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = m.GlobalNamespace.GetTypeMember("I1");

                foreach (var tuple in new[] { (name: "M1", access: Accessibility.Public), (name: "M2", access: Accessibility.Internal), (name: "M3", access: Accessibility.Private) })
                {
                    var m1 = i1.GetMember<MethodSymbol>(tuple.name);

                    Assert.False(m1.IsAbstract);
                    Assert.False(m1.IsVirtual);
                    Assert.False(m1.IsMetadataVirtual());
                    Assert.False(m1.IsSealed);
                    Assert.True(m1.IsStatic);
                    Assert.False(m1.IsExtern);
                    Assert.False(m1.IsAsync);
                    Assert.False(m1.IsOverride);
                    Assert.Equal(tuple.access, m1.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(m1));
                }
            }
        }

        [Fact]
        public void MethodModifiers_07()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M1(); 

    virtual static void M2() 
    {
    }

    sealed static void M3() 
    {
    }

    static void M4() 
    {
    }
}

class Test1 : I1
{
    void I1.M4() {}
    void I1.M1() {}
    void I1.M2() {}
    void I1.M3() {}
}

class Test2 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (10,24): error CS0238: 'I1.M3()' cannot be sealed because it is not an override
                //     sealed static void M3() 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "M3").WithArguments("I1.M3()").WithLocation(10, 24),
                // (6,25): error CS0112: A static member 'I1.M2()' cannot be marked as override, virtual, or abstract
                //     virtual static void M2() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M2").WithArguments("I1.M2()").WithLocation(6, 25),
                // (4,26): error CS0112: A static member 'I1.M1()' cannot be marked as override, virtual, or abstract
                //     abstract static void M1(); 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M1").WithArguments("I1.M1()").WithLocation(4, 26),
                // (21,13): error CS0539: 'Test1.M4()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M4() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M4").WithArguments("Test1.M4()").WithLocation(21, 13),
                // (22,13): error CS0539: 'Test1.M1()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M1() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M1").WithArguments("Test1.M1()").WithLocation(22, 13),
                // (23,13): error CS0539: 'Test1.M2()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M2() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M2").WithArguments("Test1.M2()").WithLocation(23, 13),
                // (24,13): error CS0539: 'Test1.M3()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M3() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M3").WithArguments("Test1.M3()").WithLocation(24, 13)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m1 = i1.GetMember<MethodSymbol>("M1");

            Assert.True(m1.IsAbstract);
            Assert.False(m1.IsVirtual);
            Assert.True(m1.IsMetadataVirtual());
            Assert.False(m1.IsSealed);
            Assert.True(m1.IsStatic);
            Assert.False(m1.IsExtern);
            Assert.False(m1.IsAsync);
            Assert.False(m1.IsOverride);
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m1));

            var m2 = i1.GetMember<MethodSymbol>("M2");

            Assert.False(m2.IsAbstract);
            Assert.True(m2.IsVirtual);
            Assert.True(m2.IsMetadataVirtual());
            Assert.False(m2.IsSealed);
            Assert.True(m2.IsStatic);
            Assert.False(m2.IsExtern);
            Assert.False(m2.IsAsync);
            Assert.False(m2.IsOverride);
            Assert.Equal(Accessibility.Public, m2.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m2));

            var m3 = i1.GetMember<MethodSymbol>("M3");

            Assert.False(m3.IsAbstract);
            Assert.False(m3.IsVirtual);
            Assert.False(m3.IsMetadataVirtual());
            Assert.True(m3.IsSealed);
            Assert.True(m3.IsStatic);
            Assert.False(m3.IsExtern);
            Assert.False(m3.IsAsync);
            Assert.False(m3.IsOverride);
            Assert.Equal(Accessibility.Public, m3.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m3));
        }

        [Fact]
        public void MethodModifiers_08()
        {
            var source1 =
@"
public interface I1
{
    private void M1() 
    {
        System.Console.WriteLine(""M1"");
    }

    void M4()
    {
        System.Console.WriteLine(""M4"");
        M1();
    }
}

class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.M4();
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1/*, expectedOutput:
@"M4
M1"*/, verify:false, symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = m.GlobalNamespace.GetTypeMember("I1");
                var m1 = i1.GetMember<MethodSymbol>("M1");

                Assert.False(m1.IsAbstract);
                Assert.False(m1.IsVirtual);
                Assert.False(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(Accessibility.Private, m1.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(m1));
            }
        }

        [Fact]
        public void MethodModifiers_09()
        {
            var source1 =
@"
public interface I1
{
    abstract private void M1(); 

    virtual private void M2() 
    {
    }

    sealed private void M3() 
    {
    }
}

class Test1 : I1
{
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (10,25): error CS0238: 'I1.M3()' cannot be sealed because it is not an override
                //     sealed private void M3() 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "M3").WithArguments("I1.M3()").WithLocation(10, 25),
                // (6,26): error CS0621: 'I1.M2()': virtual or abstract members cannot be private
                //     virtual private void M2() 
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "M2").WithArguments("I1.M2()").WithLocation(6, 26),
                // (4,27): error CS0621: 'I1.M1()': virtual or abstract members cannot be private
                //     abstract private void M1(); 
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "M1").WithArguments("I1.M1()").WithLocation(4, 27),
                // (15,15): error CS0535: 'Test1' does not implement interface member 'I1.M1()'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.M1()").WithLocation(15, 15)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m1 = i1.GetMember<MethodSymbol>("M1");

            Assert.True(m1.IsAbstract);
            Assert.False(m1.IsVirtual);
            Assert.True(m1.IsMetadataVirtual());
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsStatic);
            Assert.False(m1.IsExtern);
            Assert.False(m1.IsAsync);
            Assert.False(m1.IsOverride);
            Assert.Equal(Accessibility.Private, m1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m1));

            var m2 = i1.GetMember<MethodSymbol>("M2");

            Assert.False(m2.IsAbstract);
            Assert.True(m2.IsVirtual);
            Assert.True(m2.IsMetadataVirtual());
            Assert.False(m2.IsSealed);
            Assert.False(m2.IsStatic);
            Assert.False(m2.IsExtern);
            Assert.False(m2.IsAsync);
            Assert.False(m2.IsOverride);
            Assert.Equal(Accessibility.Private, m2.DeclaredAccessibility);
            Assert.Same(m2, test1.FindImplementationForInterfaceMember(m2));

            var m3 = i1.GetMember<MethodSymbol>("M3");

            Assert.False(m3.IsAbstract);
            Assert.False(m3.IsVirtual);
            Assert.False(m3.IsMetadataVirtual());
            Assert.True(m3.IsSealed);
            Assert.False(m3.IsStatic);
            Assert.False(m3.IsExtern);
            Assert.False(m3.IsAsync);
            Assert.False(m3.IsOverride);
            Assert.Equal(Accessibility.Private, m3.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m3));
        }

        [Fact]
        public void MethodModifiers_10()
        {
            var source1 =
@"
public interface I1
{
    internal abstract void M1(); 

    void M2() {M1();}
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.M2();
    }

    public void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}
";
            var compilation1 = CreateStandardCompilation(source1 + source2, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1/*, expectedOutput:"M1"*/, verify:false, symbolValidator: Validate1);

            Validate1(compilation1.SourceModule);

            void Validate1(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = test1.Interfaces.Single();
                var m1 = i1.GetMember<MethodSymbol>("M1");

                ValidateMethod(m1);
                Assert.Same(test1.GetMember("M1"), test1.FindImplementationForInterfaceMember(m1));
            }

            void ValidateMethod(MethodSymbol m1)
            {
                Assert.True(m1.IsAbstract);
                Assert.False(m1.IsVirtual);
                Assert.True(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(Accessibility.Internal, m1.DeclaredAccessibility);
            }

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            {
                var i1 = compilation2.GetTypeByMetadataName("I1");
                ValidateMethod(i1.GetMember<MethodSymbol>("M1"));
            }

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation2.ToMetadataReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation3/*, expectedOutput:"M1"*/, verify: false, symbolValidator: Validate1);

            Validate1(compilation3.SourceModule);

            var compilation4 = CreateStandardCompilation(source2, new[] { compilation2.EmitToImageReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation4.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation4/*, expectedOutput:"M1"*/, verify: false, symbolValidator: Validate1);

            Validate1(compilation4.SourceModule); 

            var source3 =
@"
class Test2 : I1
{
}
";

            var compilation5 = CreateStandardCompilation(source3, new[] { compilation2.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation5.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation5.VerifyDiagnostics(
                // (2,15): error CS0535: 'Test2' does not implement interface member 'I1.M1()'
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.M1()").WithLocation(2, 15)
                );

            {
                var test2 = compilation5.GetTypeByMetadataName("Test2");
                var i1 = compilation5.GetTypeByMetadataName("I1");
                var m1 = i1.GetMember<MethodSymbol>("M1");
                Assert.Null(test2.FindImplementationForInterfaceMember(m1));
            }

            var compilation6 = CreateStandardCompilation(source3, new[] { compilation2.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation6.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation6.VerifyDiagnostics(
                // (2,15): error CS0535: 'Test2' does not implement interface member 'I1.M1()'
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.M1()").WithLocation(2, 15)
                );

            {
                var test2 = compilation6.GetTypeByMetadataName("Test2");
                var i1 = compilation6.GetTypeByMetadataName("I1");
                var m1 = i1.GetMember<MethodSymbol>("M1");
                Assert.Null(test2.FindImplementationForInterfaceMember(m1));
            }
        }

        [Fact]
        public void MethodModifiers_11()
        {
            var source1 =
@"
public interface I1
{
    internal abstract void M1(); 
}

class Test1 : I1
{
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (7,15): error CS0535: 'Test1' does not implement interface member 'I1.M1()'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.M1()").WithLocation(7, 15)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m1 = i1.GetMember<MethodSymbol>("M1");

            Assert.True(m1.IsAbstract);
            Assert.False(m1.IsVirtual);
            Assert.True(m1.IsMetadataVirtual());
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsStatic);
            Assert.False(m1.IsExtern);
            Assert.False(m1.IsAsync);
            Assert.False(m1.IsOverride);
            Assert.Equal(Accessibility.Internal, m1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m1));
        }

        [Fact]
        public void MethodModifiers_12()
        {
            var source1 =
@"
public interface I1
{
    public sealed void M1() 
    {
        System.Console.WriteLine(""M1"");
    }
}

class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.M1();
    }

    public void M1() 
    {
        System.Console.WriteLine(""Test1.M1"");
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = m.GlobalNamespace.GetTypeMember("I1");
                var m1 = i1.GetMember<MethodSymbol>("M1");

                Assert.False(m1.IsAbstract);
                Assert.False(m1.IsVirtual);
                Assert.False(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(m1));
            }

            CompileAndVerify(compilation1/*, expectedOutput:"M1"*/, verify: false, symbolValidator: Validate);
            Validate(compilation1.SourceModule);
        }

        [Fact]
        public void MethodModifiers_13()
        {
            var source1 =
@"
public interface I1
{
    public sealed void M1() 
    {
        System.Console.WriteLine(""M1"");
    }

    abstract sealed void M2(); 

    virtual sealed void M3() 
    {
    }

    public sealed void M4();
}

class Test1 : I1
{
    void I1.M1() {}
    void I1.M2() {}
    void I1.M3() {}
    void I1.M4() {}
}

class Test2 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (15,24): error CS0501: 'I1.M4()' must declare a body because it is not marked abstract, extern, or partial
                //     public sealed void M4();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M4").WithArguments("I1.M4()").WithLocation(15, 24),
                // (9,26): error CS0238: 'I1.M2()' cannot be sealed because it is not an override
                //     abstract sealed void M2(); 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "M2").WithArguments("I1.M2()").WithLocation(9, 26),
                // (11,25): error CS0238: 'I1.M3()' cannot be sealed because it is not an override
                //     virtual sealed void M3() 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "M3").WithArguments("I1.M3()").WithLocation(11, 25),
                // (26,15): error CS0535: 'Test2' does not implement interface member 'I1.M2()'
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.M2()").WithLocation(26, 15),
                // (23,13): error CS0539: 'Test1.M4()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M4() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M4").WithArguments("Test1.M4()").WithLocation(23, 13),
                // (20,13): error CS0539: 'Test1.M1()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M1() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M1").WithArguments("Test1.M1()").WithLocation(20, 13)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m1 = i1.GetMember<MethodSymbol>("M1");

            Assert.False(m1.IsAbstract);
            Assert.False(m1.IsVirtual);
            Assert.False(m1.IsMetadataVirtual());
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsStatic);
            Assert.False(m1.IsExtern);
            Assert.False(m1.IsAsync);
            Assert.False(m1.IsOverride);
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m1));
            Assert.Null(test2.FindImplementationForInterfaceMember(m1));

            var m2 = i1.GetMember<MethodSymbol>("M2");

            Assert.True(m2.IsAbstract);
            Assert.False(m2.IsVirtual);
            Assert.True(m2.IsMetadataVirtual());
            Assert.True(m2.IsSealed);
            Assert.False(m2.IsStatic);
            Assert.False(m2.IsExtern);
            Assert.False(m2.IsAsync);
            Assert.False(m2.IsOverride);
            Assert.Equal(Accessibility.Public, m2.DeclaredAccessibility);
            Assert.Same(test1.GetMember("I1.M2"), test1.FindImplementationForInterfaceMember(m2));
            Assert.Null(test2.FindImplementationForInterfaceMember(m2));

            var m3 = i1.GetMember<MethodSymbol>("M3");

            Assert.False(m3.IsAbstract);
            Assert.True(m3.IsVirtual);
            Assert.True(m3.IsMetadataVirtual());
            Assert.True(m3.IsSealed);
            Assert.False(m3.IsStatic);
            Assert.False(m3.IsExtern);
            Assert.False(m3.IsAsync);
            Assert.False(m3.IsOverride);
            Assert.Equal(Accessibility.Public, m3.DeclaredAccessibility);
            Assert.Same(test1.GetMember("I1.M3"), test1.FindImplementationForInterfaceMember(m3));
            Assert.Same(m3, test2.FindImplementationForInterfaceMember(m3));

            var m4 = i1.GetMember<MethodSymbol>("M4");

            Assert.False(m4.IsAbstract);
            Assert.False(m4.IsVirtual);
            Assert.False(m4.IsMetadataVirtual());
            Assert.False(m4.IsSealed);
            Assert.False(m4.IsStatic);
            Assert.False(m4.IsExtern);
            Assert.False(m4.IsAsync);
            Assert.False(m4.IsOverride);
            Assert.Equal(Accessibility.Public, m4.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m4));
            Assert.Null(test2.FindImplementationForInterfaceMember(m4));
        }

        [Fact]
        public void MethodModifiers_14()
        {
            var source1 =
@"
public interface I1
{
    abstract virtual void M2(); 

    virtual abstract void M3() 
    {
    }
}

class Test1 : I1
{
    void I1.M2() {}
    void I1.M3() {}
}

class Test2 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (6,27): error CS0500: 'I1.M3()' cannot declare a body because it is marked abstract
                //     virtual abstract void M3() 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M3").WithArguments("I1.M3()").WithLocation(6, 27),
                // (6,27): error CS0503: The abstract method 'I1.M3()' cannot be marked virtual
                //     virtual abstract void M3() 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "M3").WithArguments("I1.M3()").WithLocation(6, 27),
                // (4,27): error CS0503: The abstract method 'I1.M2()' cannot be marked virtual
                //     abstract virtual void M2(); 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "M2").WithArguments("I1.M2()").WithLocation(4, 27),
                // (17,15): error CS0535: 'Test2' does not implement interface member 'I1.M3()'
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.M3()").WithLocation(17, 15),
                // (17,15): error CS0535: 'Test2' does not implement interface member 'I1.M2()'
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.M2()").WithLocation(17, 15)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");
            var i1 = compilation1.GetTypeByMetadataName("I1");

            foreach (var methodName in new[] { "M2", "M3" })
            {
                var m2 = i1.GetMember<MethodSymbol>(methodName);

                Assert.True(m2.IsAbstract);
                Assert.True(m2.IsVirtual);
                Assert.True(m2.IsMetadataVirtual());
                Assert.False(m2.IsSealed);
                Assert.False(m2.IsStatic);
                Assert.False(m2.IsExtern);
                Assert.False(m2.IsAsync);
                Assert.False(m2.IsOverride);
                Assert.Equal(Accessibility.Public, m2.DeclaredAccessibility);
                Assert.Same(test1.GetMember("I1." + methodName), test1.FindImplementationForInterfaceMember(m2));
                Assert.Null(test2.FindImplementationForInterfaceMember(m2));
            }
        }

        [Fact]
        public void MethodModifiers_15()
        {
            var source1 =
@"
public interface I1
{
    extern void M1(); 
    virtual extern void M2(); 
    static extern void M3(); 
    private extern void M4();
    extern sealed void M5();
}

class Test1 : I1
{
}

class Test2 : I1
{
    void I1.M1() {}
    void I1.M2() {}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var test2 = m.GlobalNamespace.GetTypeMember("Test2");
                var i1 = m.GlobalNamespace.GetTypeMember("I1");
                var m1 = i1.GetMember<MethodSymbol>("M1");
                bool isSource = !(m is PEModuleSymbol);

                Assert.False(m1.IsAbstract);
                Assert.True(m1.IsVirtual);
                Assert.True(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.Equal(isSource, m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
                Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));
                Assert.Same(test2.GetMember("I1.M1"), test2.FindImplementationForInterfaceMember(m1));

                var m2 = i1.GetMember<MethodSymbol>("M2");

                Assert.False(m2.IsAbstract);
                Assert.True(m2.IsVirtual);
                Assert.True(m2.IsMetadataVirtual());
                Assert.False(m2.IsSealed);
                Assert.False(m2.IsStatic);
                Assert.Equal(isSource, m2.IsExtern);
                Assert.False(m2.IsAsync);
                Assert.False(m2.IsOverride);
                Assert.Equal(Accessibility.Public, m2.DeclaredAccessibility);
                Assert.Same(m2, test1.FindImplementationForInterfaceMember(m2));
                Assert.Same(test2.GetMember("I1.M2"), test2.FindImplementationForInterfaceMember(m2));

                var m3 = i1.GetMember<MethodSymbol>("M3");

                Assert.False(m3.IsAbstract);
                Assert.False(m3.IsVirtual);
                Assert.False(m3.IsMetadataVirtual());
                Assert.False(m3.IsSealed);
                Assert.True(m3.IsStatic);
                Assert.Equal(isSource, m3.IsExtern);
                Assert.False(m3.IsAsync);
                Assert.False(m3.IsOverride);
                Assert.Equal(Accessibility.Public, m3.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(m3));
                Assert.Null(test2.FindImplementationForInterfaceMember(m3));

                var m4 = i1.GetMember<MethodSymbol>("M4");

                Assert.False(m4.IsAbstract);
                Assert.False(m4.IsVirtual);
                Assert.False(m4.IsMetadataVirtual());
                Assert.False(m4.IsSealed);
                Assert.False(m4.IsStatic);
                Assert.Equal(isSource, m4.IsExtern);
                Assert.False(m4.IsAsync);
                Assert.False(m4.IsOverride);
                Assert.Equal(Accessibility.Private, m4.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(m4));
                Assert.Null(test2.FindImplementationForInterfaceMember(m4));

                var m5 = i1.GetMember<MethodSymbol>("M5");

                Assert.False(m5.IsAbstract);
                Assert.False(m5.IsVirtual);
                Assert.False(m5.IsMetadataVirtual());
                Assert.False(m5.IsSealed);
                Assert.False(m5.IsStatic);
                Assert.Equal(isSource, m5.IsExtern);
                Assert.False(m5.IsAsync);
                Assert.False(m5.IsOverride);
                Assert.Equal(Accessibility.Public, m5.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(m5));
                Assert.Null(test2.FindImplementationForInterfaceMember(m5));
            }
        }

        [Fact]
        public void MethodModifiers_16()
        {
            var source1 =
@"
public interface I1
{
    abstract extern void M1(); 
    extern void M2() {} 
    static extern void M3(); 
    private extern void M4();
    extern sealed void M5();
}

class Test1 : I1
{
}

class Test2 : I1
{
    void I1.M1() {}
    void I1.M2() {}
    void I1.M3() {}
    void I1.M4() {}
    void I1.M5() {}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,26): error CS0180: 'I1.M1()' cannot be both extern and abstract
                //     abstract extern void M1(); 
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "M1").WithArguments("I1.M1()").WithLocation(4, 26),
                // (5,17): error CS0179: 'I1.M2()' cannot be extern and declare a body
                //     extern void M2() {} 
                Diagnostic(ErrorCode.ERR_ExternHasBody, "M2").WithArguments("I1.M2()").WithLocation(5, 17),
                // (11,15): error CS0535: 'Test1' does not implement interface member 'I1.M1()'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.M1()").WithLocation(11, 15),
                // (19,13): error CS0539: 'Test2.M3()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M3() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M3").WithArguments("Test2.M3()").WithLocation(19, 13),
                // (20,13): error CS0539: 'Test2.M4()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M4() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M4").WithArguments("Test2.M4()").WithLocation(20, 13),
                // (21,13): error CS0539: 'Test2.M5()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M5() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M5").WithArguments("Test2.M5()").WithLocation(21, 13),
                // (6,24): warning CS0626: Method, operator, or accessor 'I1.M3()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     static extern void M3(); 
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "M3").WithArguments("I1.M3()").WithLocation(6, 24),
                // (7,25): warning CS0626: Method, operator, or accessor 'I1.M4()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     private extern void M4();
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "M4").WithArguments("I1.M4()").WithLocation(7, 25),
                // (8,24): warning CS0626: Method, operator, or accessor 'I1.M5()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern sealed void M5();
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "M5").WithArguments("I1.M5()").WithLocation(8, 24)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m1 = i1.GetMember<MethodSymbol>("M1");

            Assert.True(m1.IsAbstract);
            Assert.False(m1.IsVirtual);
            Assert.True(m1.IsMetadataVirtual());
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsStatic);
            Assert.True(m1.IsExtern);
            Assert.False(m1.IsAsync);
            Assert.False(m1.IsOverride);
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m1));
            Assert.Same(test2.GetMember("I1.M1"), test2.FindImplementationForInterfaceMember(m1));

            var m2 = i1.GetMember<MethodSymbol>("M2");

            Assert.False(m2.IsAbstract);
            Assert.True(m2.IsVirtual);
            Assert.True(m2.IsMetadataVirtual());
            Assert.False(m2.IsSealed);
            Assert.False(m2.IsStatic);
            Assert.True(m2.IsExtern);
            Assert.False(m2.IsAsync);
            Assert.False(m2.IsOverride);
            Assert.Equal(Accessibility.Public, m2.DeclaredAccessibility);
            Assert.Same(m2, test1.FindImplementationForInterfaceMember(m2));
            Assert.Same(test2.GetMember("I1.M2"), test2.FindImplementationForInterfaceMember(m2));

            var m3 = i1.GetMember<MethodSymbol>("M3");
            Assert.Null(test2.FindImplementationForInterfaceMember(m3));

            var m4 = i1.GetMember<MethodSymbol>("M4");
            Assert.Null(test2.FindImplementationForInterfaceMember(m4));

            var m5 = i1.GetMember<MethodSymbol>("M5");
            Assert.Null(test2.FindImplementationForInterfaceMember(m5));
        }

        [Fact]
        public void MethodModifiers_17()
        {
            var source1 =
@"
public interface I1
{
    abstract void M1() {} 
    abstract private void M2() {} 
    abstract static void M3() {} 
    static extern void M4() {}
    override sealed void M5() {}
}

class Test1 : I1
{
}

class Test2 : I1
{
    void I1.M1() {}
    void I1.M2() {}
    void I1.M3() {}
    void I1.M4() {}
    void I1.M5() {}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,19): error CS0500: 'I1.M1()' cannot declare a body because it is marked abstract
                //     abstract void M1() {} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M1").WithArguments("I1.M1()").WithLocation(4, 19),
                // (5,27): error CS0500: 'I1.M2()' cannot declare a body because it is marked abstract
                //     abstract private void M2() {} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M2").WithArguments("I1.M2()").WithLocation(5, 27),
                // (6,26): error CS0500: 'I1.M3()' cannot declare a body because it is marked abstract
                //     abstract static void M3() {} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M3").WithArguments("I1.M3()").WithLocation(6, 26),
                // (7,24): error CS0179: 'I1.M4()' cannot be extern and declare a body
                //     static extern void M4() {}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "M4").WithArguments("I1.M4()").WithLocation(7, 24),
                // (8,26): error CS0106: The modifier 'override' is not valid for this item
                //     override sealed void M5() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M5").WithArguments("override").WithLocation(8, 26),
                // (5,27): error CS0621: 'I1.M2()': virtual or abstract members cannot be private
                //     abstract private void M2() {} 
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "M2").WithArguments("I1.M2()").WithLocation(5, 27),
                // (6,26): error CS0112: A static member 'I1.M3()' cannot be marked as override, virtual, or abstract
                //     abstract static void M3() {} 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M3").WithArguments("I1.M3()").WithLocation(6, 26),
                // (11,15): error CS0535: 'Test1' does not implement interface member 'I1.M2()'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.M2()").WithLocation(11, 15),
                // (11,15): error CS0535: 'Test1' does not implement interface member 'I1.M1()'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.M1()").WithLocation(11, 15),
                // (19,13): error CS0539: 'Test2.M3()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M3() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M3").WithArguments("Test2.M3()").WithLocation(19, 13),
                // (20,13): error CS0539: 'Test2.M4()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M4() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M4").WithArguments("Test2.M4()").WithLocation(20, 13),
                // (21,13): error CS0539: 'Test2.M5()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.M5() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M5").WithArguments("Test2.M5()").WithLocation(21, 13)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m1 = i1.GetMember<MethodSymbol>("M1");

            Assert.True(m1.IsAbstract);
            Assert.False(m1.IsVirtual);
            Assert.True(m1.IsMetadataVirtual());
            Assert.False(m1.IsSealed);
            Assert.False(m1.IsStatic);
            Assert.False(m1.IsExtern);
            Assert.False(m1.IsAsync);
            Assert.False(m1.IsOverride);
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m1));
            Assert.Same(test2.GetMember("I1.M1"), test2.FindImplementationForInterfaceMember(m1));

            var m2 = i1.GetMember<MethodSymbol>("M2");

            Assert.True(m2.IsAbstract);
            Assert.False(m2.IsVirtual);
            Assert.True(m2.IsMetadataVirtual());
            Assert.False(m2.IsSealed);
            Assert.False(m2.IsStatic);
            Assert.False(m2.IsExtern);
            Assert.False(m2.IsAsync);
            Assert.False(m2.IsOverride);
            Assert.Equal(Accessibility.Private, m2.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m2));
            Assert.Same(test2.GetMember("I1.M2"), test2.FindImplementationForInterfaceMember(m2));

            var m3 = i1.GetMember<MethodSymbol>("M3");

            Assert.True(m3.IsAbstract);
            Assert.False(m3.IsVirtual);
            Assert.True(m3.IsMetadataVirtual());
            Assert.False(m3.IsSealed);
            Assert.True(m3.IsStatic);
            Assert.False(m3.IsExtern);
            Assert.False(m3.IsAsync);
            Assert.False(m3.IsOverride);
            Assert.Equal(Accessibility.Public, m3.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m3));
            Assert.Null(test2.FindImplementationForInterfaceMember(m3));

            var m4 = i1.GetMember<MethodSymbol>("M4");

            Assert.False(m4.IsAbstract);
            Assert.False(m4.IsVirtual);
            Assert.False(m4.IsMetadataVirtual());
            Assert.False(m4.IsSealed);
            Assert.True(m4.IsStatic);
            Assert.True(m4.IsExtern);
            Assert.False(m4.IsAsync);
            Assert.False(m4.IsOverride);
            Assert.Equal(Accessibility.Public, m4.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m4));
            Assert.Null(test2.FindImplementationForInterfaceMember(m4));

            var m5 = i1.GetMember<MethodSymbol>("M5");

            Assert.False(m5.IsAbstract);
            Assert.False(m5.IsVirtual);
            Assert.False(m5.IsMetadataVirtual());
            Assert.False(m5.IsSealed);
            Assert.False(m5.IsStatic);
            Assert.False(m5.IsExtern);
            Assert.False(m5.IsAsync);
            Assert.False(m5.IsOverride);
            Assert.Equal(Accessibility.Public, m5.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(m5));
            Assert.Null(test2.FindImplementationForInterfaceMember(m5));
        }

        [Fact]
        public void MethodModifiers_18()
        {
            var source1 =
@"
using System.Threading;
using System.Threading.Tasks;

public interface I1
{
    public static async Task M1() 
    {
        await Task.Factory.StartNew(() => System.Console.WriteLine(""M1""));
    }
}

class Test1 : I1
{
    static void Main()
    {
        I1.M1().Wait();
    }
}
";
            var compilation1 = CreateCompilationWithMscorlib45(source1, options: TestOptions.DebugExe,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, expectedOutput:"M1", symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = m.GlobalNamespace.GetTypeMember("I1");
                var m1 = i1.GetMember<MethodSymbol>("M1");

                Assert.False(m1.IsAbstract);
                Assert.False(m1.IsVirtual);
                Assert.False(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.True(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.Equal(!(m is PEModuleSymbol), m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(m1));
            }
        }

        [Fact]
        public void MethodModifiers_19()
        {
            var source1 =
@"

public interface I2 {}

public interface I1
{
    public void I2.M01();
    protected void I2.M02();
    protected internal void I2.M03();
    internal void I2.M04();
    private void I2.M05();
    static void I2.M06();
    virtual void I2.M07();
    sealed void I2.M08();
    override void I2.M09();
    abstract void I2.M10();
    extern void I2.M11();
    async void I2.M12();
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            var expected = new[]
            {
                // (7,20): error CS0106: The modifier 'public' is not valid for this item
                //     public void I2.M01();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M01").WithArguments("public").WithLocation(7, 20),
                // (8,23): error CS0106: The modifier 'protected' is not valid for this item
                //     protected void I2.M02();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M02").WithArguments("protected").WithLocation(8, 23),
                // (9,32): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal void I2.M03();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M03").WithArguments("protected internal").WithLocation(9, 32),
                // (10,22): error CS0106: The modifier 'internal' is not valid for this item
                //     internal void I2.M04();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("internal").WithLocation(10, 22),
                // (11,21): error CS0106: The modifier 'private' is not valid for this item
                //     private void I2.M05();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M05").WithArguments("private").WithLocation(11, 21),
                // (12,20): error CS0106: The modifier 'static' is not valid for this item
                //     static void I2.M06();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("static").WithLocation(12, 20),
                // (13,21): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual void I2.M07();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("virtual").WithLocation(13, 21),
                // (14,20): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed void I2.M08();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M08").WithArguments("sealed").WithLocation(14, 20),
                // (15,22): error CS0106: The modifier 'override' is not valid for this item
                //     override void I2.M09();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(15, 22),
                // (16,22): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract void I2.M10();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("abstract").WithLocation(16, 22),
                // (17,20): error CS0106: The modifier 'extern' is not valid for this item
                //     extern void I2.M11();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M11").WithArguments("extern").WithLocation(17, 20),
                // (18,19): error CS0106: The modifier 'async' is not valid for this item
                //     async void I2.M12();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M12").WithArguments("async").WithLocation(18, 19),
                // (7,20): error CS0541: 'I1.M01()': explicit interface declaration can only be declared in a class or struct
                //     public void I2.M01();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M01").WithArguments("I1.M01()").WithLocation(7, 20),
                // (8,23): error CS0541: 'I1.M02()': explicit interface declaration can only be declared in a class or struct
                //     protected void I2.M02();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M02").WithArguments("I1.M02()").WithLocation(8, 23),
                // (9,32): error CS0541: 'I1.M03()': explicit interface declaration can only be declared in a class or struct
                //     protected internal void I2.M03();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M03").WithArguments("I1.M03()").WithLocation(9, 32),
                // (10,22): error CS0541: 'I1.M04()': explicit interface declaration can only be declared in a class or struct
                //     internal void I2.M04();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M04").WithArguments("I1.M04()").WithLocation(10, 22),
                // (11,21): error CS0541: 'I1.M05()': explicit interface declaration can only be declared in a class or struct
                //     private void I2.M05();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M05").WithArguments("I1.M05()").WithLocation(11, 21),
                // (12,20): error CS0541: 'I1.M06()': explicit interface declaration can only be declared in a class or struct
                //     static void I2.M06();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M06").WithArguments("I1.M06()").WithLocation(12, 20),
                // (13,21): error CS0541: 'I1.M07()': explicit interface declaration can only be declared in a class or struct
                //     virtual void I2.M07();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M07").WithArguments("I1.M07()").WithLocation(13, 21),
                // (14,20): error CS0541: 'I1.M08()': explicit interface declaration can only be declared in a class or struct
                //     sealed void I2.M08();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M08").WithArguments("I1.M08()").WithLocation(14, 20),
                // (15,22): error CS0541: 'I1.M09()': explicit interface declaration can only be declared in a class or struct
                //     override void I2.M09();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M09").WithArguments("I1.M09()").WithLocation(15, 22),
                // (16,22): error CS0541: 'I1.M10()': explicit interface declaration can only be declared in a class or struct
                //     abstract void I2.M10();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M10").WithArguments("I1.M10()").WithLocation(16, 22),
                // (17,20): error CS0541: 'I1.M11()': explicit interface declaration can only be declared in a class or struct
                //     extern void I2.M11();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M11").WithArguments("I1.M11()").WithLocation(17, 20),
                // (18,19): error CS0541: 'I1.M12()': explicit interface declaration can only be declared in a class or struct
                //     async void I2.M12();
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M12").WithArguments("I1.M12()").WithLocation(18, 19)
            };

            compilation1.VerifyDiagnostics(expected);

            ValidateSymbolsMethodModifiers_19(compilation1);

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(expected);

            ValidateSymbolsMethodModifiers_19(compilation2);
        }

        private static void ValidateSymbolsMethodModifiers_19(CSharpCompilation compilation1)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");

            foreach (var methodName in new[] { "I2.M01", "I2.M02", "I2.M03", "I2.M04", "I2.M05", "I2.M06", "I2.M07", "I2.M08", "I2.M09", "I2.M10", "I2.M11", "I2.M12" })
            {
                var m01 = i1.GetMember<MethodSymbol>(methodName);

                Assert.True(m01.IsAbstract);
                Assert.False(m01.IsVirtual);
                Assert.True(m01.IsMetadataVirtual());
                Assert.False(m01.IsSealed);
                Assert.False(m01.IsStatic);
                Assert.False(m01.IsExtern);
                Assert.False(m01.IsAsync);
                Assert.False(m01.IsOverride);
                Assert.Equal(Accessibility.Public, m01.DeclaredAccessibility);
            }
        }

        [Fact]
        public void MethodModifiers_20()
        {
            var source1 =
@"
public interface I1
{
    internal void M1()
    {
        System.Console.WriteLine(""M1"");
    }

    void M2() {M1();}
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.M2();
    }
}
";
            var compilation1 = CreateStandardCompilation(source1 + source2, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1/*, expectedOutput:"M1"*/, verify: false, symbolValidator: Validate1);

            Validate1(compilation1.SourceModule);

            void Validate1(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = test1.Interfaces.Single();
                var m1 = i1.GetMember<MethodSymbol>("M1");

                ValidateMethod(m1);
                Assert.Same(m1, test1.FindImplementationForInterfaceMember(m1));
            }

            void ValidateMethod(MethodSymbol m1)
            {
                Assert.False(m1.IsAbstract);
                Assert.True(m1.IsVirtual);
                Assert.True(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(Accessibility.Internal, m1.DeclaredAccessibility);
            }

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            {
                var i1 = compilation2.GetTypeByMetadataName("I1");
                var m1 = i1.GetMember<MethodSymbol>("M1");
                ValidateMethod(m1);
            }

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation2.ToMetadataReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation3/*, expectedOutput:"M1"*/, verify: false, symbolValidator: Validate1);

            Validate1(compilation3.SourceModule);

            var compilation4 = CreateStandardCompilation(source2, new[] { compilation2.EmitToImageReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation4.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation4/*, expectedOutput:"M1"*/, verify: false, symbolValidator: Validate1);

            Validate1(compilation4.SourceModule);
        }

        [Fact]
        public void MethodModifiers_21()
        {
            var source1 =
@"
public interface I1
{
    private static void M1() {}

    internal static void M2() {}

    public static void M3() {}

    static void M4() {}
}

class Test1
{
    static void Main()
    {
        I1.M1();
        I1.M2();
        I1.M3();
        I1.M4();
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (17,12): error CS0122: 'I1.M1()' is inaccessible due to its protection level
                //         I1.M1();
                Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("I1.M1()").WithLocation(17, 12)
                );

            var source2 =
@"
class Test2
{
    static void Main()
    {
        I1.M1();
        I1.M2();
        I1.M3();
        I1.M4();
    }
}
";
            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() },
                                                         options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(
                // (6,12): error CS0122: 'I1.M1()' is inaccessible due to its protection level
                //         I1.M1();
                Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("I1.M1()").WithLocation(6, 12),
                // (7,12): error CS0122: 'I1.M2()' is inaccessible due to its protection level
                //         I1.M2();
                Diagnostic(ErrorCode.ERR_BadAccess, "M2").WithArguments("I1.M2()").WithLocation(7, 12)
                );
        }

        [Fact]
        public void ImplicitThisIsAllowed_03()
        {
            var source1 =
@"
public interface I1
{
    public int F1;

    void M1() 
    {
        System.Console.WriteLine(""I1.M1"");
    }

    int P1
    {
        get
        {
            System.Console.WriteLine(""I1.get_P1"");
            return 0;
        }
        set => System.Console.WriteLine(""I1.set_P1"");
    }

    event System.Action E1
    {
        add => System.Console.WriteLine(""I1.add_E1"");
        remove => System.Console.WriteLine(""I1.remove_E1"");
    }

    public interface I2 : I1
    {
        void M2() 
        {
            M1();
            P1 = P1;
            E1 += null;
            E1 -= null;
            F1 = 0;
        }
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (27,22): error CS0524: 'I1.I2': interfaces cannot declare types
                //     public interface I2 : I1
                Diagnostic(ErrorCode.ERR_InterfacesCannotContainTypes, "I2").WithArguments("I1.I2").WithLocation(27, 22),
                // (4,16): error CS0525: Interfaces cannot contain fields
                //     public int F1;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "F1").WithLocation(4, 16)
                );
        }

        [Fact]
        public void ImplicitThisIsAllowed_04()
        {
            var source1 =
@"
public interface I1
{
    public int F1;

    void M1() 
    {
        System.Console.WriteLine(""I1.M1"");
    }

    int P1
    {
        get
        {
            System.Console.WriteLine(""I1.get_P1"");
            return 0;
        }
        set => System.Console.WriteLine(""I1.set_P1"");
    }

    event System.Action E1
    {
        add => System.Console.WriteLine(""I1.add_E1"");
        remove => System.Console.WriteLine(""I1.remove_E1"");
    }

    public interface I2
    {
        void M2() 
        {
            M1();
            P1 = P1;
            E1 += null;
            E1 -= null;
            F1 = 0;
        }
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (27,22): error CS0524: 'I1.I2': interfaces cannot declare types
                //     public interface I2
                Diagnostic(ErrorCode.ERR_InterfacesCannotContainTypes, "I2").WithArguments("I1.I2").WithLocation(27, 22),
                // (4,16): error CS0525: Interfaces cannot contain fields
                //     public int F1;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "F1").WithLocation(4, 16),
                // (31,13): error CS0120: An object reference is required for the non-static field, method, or property 'I1.M1()'
                //             M1();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "M1").WithArguments("I1.M1()").WithLocation(31, 13),
                // (32,13): error CS0120: An object reference is required for the non-static field, method, or property 'I1.P1'
                //             P1 = P1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P1").WithArguments("I1.P1").WithLocation(32, 13),
                // (32,18): error CS0120: An object reference is required for the non-static field, method, or property 'I1.P1'
                //             P1 = P1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P1").WithArguments("I1.P1").WithLocation(32, 18),
                // (33,13): error CS0120: An object reference is required for the non-static field, method, or property 'I1.E1'
                //             E1 += null;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "E1").WithArguments("I1.E1").WithLocation(33, 13),
                // (34,13): error CS0120: An object reference is required for the non-static field, method, or property 'I1.E1'
                //             E1 -= null;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "E1").WithArguments("I1.E1").WithLocation(34, 13),
                // (35,13): error CS0120: An object reference is required for the non-static field, method, or property 'I1.F1'
                //             F1 = 0;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "F1").WithArguments("I1.F1").WithLocation(35, 13)
                );
        }

        [Fact]
        public void ImplicitThisIsAllowed_05()
        {
            var source1 =
@"
public class C1
{
    public int F1;

    void M1() 
    {
        System.Console.WriteLine(""I1.M1"");
    }

    int P1
    {
        get
        {
            System.Console.WriteLine(""I1.get_P1"");
            return 0;
        }
        set => System.Console.WriteLine(""I1.set_P1"");
    }

    event System.Action E1
    {
        add => System.Console.WriteLine(""I1.add_E1"");
        remove => System.Console.WriteLine(""I1.remove_E1"");
    }

    public interface I2
    {
        void M2() 
        {
            M1();
            P1 = P1;
            E1 += null;
            E1 -= null;
            F1 = 0;
        }
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            compilation1.VerifyDiagnostics(
                // (31,13): error CS0120: An object reference is required for the non-static field, method, or property 'C1.M1()'
                //             M1();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "M1").WithArguments("C1.M1()").WithLocation(31, 13),
                // (32,13): error CS0120: An object reference is required for the non-static field, method, or property 'C1.P1'
                //             P1 = P1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P1").WithArguments("C1.P1").WithLocation(32, 13),
                // (32,18): error CS0120: An object reference is required for the non-static field, method, or property 'C1.P1'
                //             P1 = P1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P1").WithArguments("C1.P1").WithLocation(32, 18),
                // (33,13): error CS0120: An object reference is required for the non-static field, method, or property 'C1.E1'
                //             E1 += null;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "E1").WithArguments("C1.E1").WithLocation(33, 13),
                // (34,13): error CS0120: An object reference is required for the non-static field, method, or property 'C1.E1'
                //             E1 -= null;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "E1").WithArguments("C1.E1").WithLocation(34, 13),
                // (35,13): error CS0120: An object reference is required for the non-static field, method, or property 'C1.F1'
                //             F1 = 0;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "F1").WithArguments("C1.F1").WithLocation(35, 13)
                );
        }

        [Fact]
        public void PropertyModifiers_01()
        {
            var source1 =
@"
public interface I1
{
    public int P01 {get; set;}
    protected int P02 {get;}
    protected internal int P03 {set;}
    internal int P04 {get;}
    private int P05 {set;}
    static int P06 {get;}
    virtual int P07 {set;}
    sealed int P08 {get;}
    override int P09 {set;}
    abstract int P10 {get;}
    extern int P11 {get; set;}

    int P12 { public get; set;}
    int P13 { get; protected set;}
    int P14 { protected internal get; set;}
    int P15 { get; internal set;}
    int P16 { private get; set;}
    int P17 { private get;}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (5,19): error CS0106: The modifier 'protected' is not valid for this item
                //     protected int P02 {get;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P02").WithArguments("protected").WithLocation(5, 19),
                // (6,28): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal int P03 {set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P03").WithArguments("protected internal").WithLocation(6, 28),
                // (8,22): error CS0501: 'I1.P05.set' must declare a body because it is not marked abstract, extern, or partial
                //     private int P05 {set;}
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I1.P05.set").WithLocation(8, 22),
                // (9,21): error CS0501: 'I1.P06.get' must declare a body because it is not marked abstract, extern, or partial
                //     static int P06 {get;}
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P06.get").WithLocation(9, 21),
                // (10,22): error CS0501: 'I1.P07.set' must declare a body because it is not marked abstract, extern, or partial
                //     virtual int P07 {set;}
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I1.P07.set").WithLocation(10, 22),
                // (11,21): error CS0501: 'I1.P08.get' must declare a body because it is not marked abstract, extern, or partial
                //     sealed int P08 {get;}
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P08.get").WithLocation(11, 21),
                // (12,18): error CS0106: The modifier 'override' is not valid for this item
                //     override int P09 {set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P09").WithArguments("override").WithLocation(12, 18),
                // (16,22): error CS0273: The accessibility modifier of the 'I1.P12.get' accessor must be more restrictive than the property or indexer 'I1.P12'
                //     int P12 { public get; set;}
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("I1.P12.get", "I1.P12").WithLocation(16, 22),
                // (17,30): error CS0106: The modifier 'protected' is not valid for this item
                //     int P13 { get; protected set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("protected").WithLocation(17, 30),
                // (18,34): error CS0106: The modifier 'protected internal' is not valid for this item
                //     int P14 { protected internal get; set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("protected internal").WithLocation(18, 34),
                // (20,23): error CS0442: 'I1.P16.get': abstract properties cannot have private accessors
                //     int P16 { private get; set;}
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "get").WithArguments("I1.P16.get").WithLocation(20, 23),
                // (21,9): error CS0276: 'I1.P17': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     int P17 { private get;}
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "P17").WithArguments("I1.P17").WithLocation(21, 9),
                // (14,21): warning CS0626: Method, operator, or accessor 'I1.P11.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern int P11 {get; set;}
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("I1.P11.get").WithLocation(14, 21),
                // (14,26): warning CS0626: Method, operator, or accessor 'I1.P11.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern int P11 {get; set;}
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("I1.P11.set").WithLocation(14, 26)
                );

            ValidateSymbolsPropertyModifiers_01(compilation1);
        }

        private static void ValidateSymbolsPropertyModifiers_01(CSharpCompilation compilation1)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p01 = i1.GetMember<PropertySymbol>("P01");

            Assert.True(p01.IsAbstract);
            Assert.False(p01.IsVirtual);
            Assert.False(p01.IsSealed);
            Assert.False(p01.IsStatic);
            Assert.False(p01.IsExtern);
            Assert.False(p01.IsOverride);
            Assert.Equal(Accessibility.Public, p01.DeclaredAccessibility);

            VaidateP01Accessor(p01.GetMethod);
            VaidateP01Accessor(p01.SetMethod);
            void VaidateP01Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p02 = i1.GetMember<PropertySymbol>("P02");
            var p02get = p02.GetMethod;

            Assert.True(p02.IsAbstract);
            Assert.False(p02.IsVirtual);
            Assert.False(p02.IsSealed);
            Assert.False(p02.IsStatic);
            Assert.False(p02.IsExtern);
            Assert.False(p02.IsOverride);
            Assert.Equal(Accessibility.Public, p02.DeclaredAccessibility);

            Assert.True(p02get.IsAbstract);
            Assert.False(p02get.IsVirtual);
            Assert.True(p02get.IsMetadataVirtual());
            Assert.False(p02get.IsSealed);
            Assert.False(p02get.IsStatic);
            Assert.False(p02get.IsExtern);
            Assert.False(p02get.IsAsync);
            Assert.False(p02get.IsOverride);
            Assert.Equal(Accessibility.Public, p02get.DeclaredAccessibility);

            var p03 = i1.GetMember<PropertySymbol>("P03");
            var p03set = p03.SetMethod;

            Assert.True(p03.IsAbstract);
            Assert.False(p03.IsVirtual);
            Assert.False(p03.IsSealed);
            Assert.False(p03.IsStatic);
            Assert.False(p03.IsExtern);
            Assert.False(p03.IsOverride);
            Assert.Equal(Accessibility.Public, p03.DeclaredAccessibility);

            Assert.True(p03set.IsAbstract);
            Assert.False(p03set.IsVirtual);
            Assert.True(p03set.IsMetadataVirtual());
            Assert.False(p03set.IsSealed);
            Assert.False(p03set.IsStatic);
            Assert.False(p03set.IsExtern);
            Assert.False(p03set.IsAsync);
            Assert.False(p03set.IsOverride);
            Assert.Equal(Accessibility.Public, p03set.DeclaredAccessibility);

            var p04 = i1.GetMember<PropertySymbol>("P04");
            var p04get = p04.GetMethod;

            Assert.True(p04.IsAbstract);
            Assert.False(p04.IsVirtual);
            Assert.False(p04.IsSealed);
            Assert.False(p04.IsStatic);
            Assert.False(p04.IsExtern);
            Assert.False(p04.IsOverride);
            Assert.Equal(Accessibility.Internal, p04.DeclaredAccessibility);

            Assert.True(p04get.IsAbstract);
            Assert.False(p04get.IsVirtual);
            Assert.True(p04get.IsMetadataVirtual());
            Assert.False(p04get.IsSealed);
            Assert.False(p04get.IsStatic);
            Assert.False(p04get.IsExtern);
            Assert.False(p04get.IsAsync);
            Assert.False(p04get.IsOverride);
            Assert.Equal(Accessibility.Internal, p04get.DeclaredAccessibility);

            var p05 = i1.GetMember<PropertySymbol>("P05");
            var p05set = p05.SetMethod;

            Assert.False(p05.IsAbstract);
            Assert.False(p05.IsVirtual);
            Assert.False(p05.IsSealed);
            Assert.False(p05.IsStatic);
            Assert.False(p05.IsExtern);
            Assert.False(p05.IsOverride);
            Assert.Equal(Accessibility.Private, p05.DeclaredAccessibility);

            Assert.False(p05set.IsAbstract);
            Assert.False(p05set.IsVirtual);
            Assert.False(p05set.IsMetadataVirtual());
            Assert.False(p05set.IsSealed);
            Assert.False(p05set.IsStatic);
            Assert.False(p05set.IsExtern);
            Assert.False(p05set.IsAsync);
            Assert.False(p05set.IsOverride);
            Assert.Equal(Accessibility.Private, p05set.DeclaredAccessibility);

            var p06 = i1.GetMember<PropertySymbol>("P06");
            var p06get = p06.GetMethod;

            Assert.False(p06.IsAbstract);
            Assert.False(p06.IsVirtual);
            Assert.False(p06.IsSealed);
            Assert.True(p06.IsStatic);
            Assert.False(p06.IsExtern);
            Assert.False(p06.IsOverride);
            Assert.Equal(Accessibility.Public, p06.DeclaredAccessibility);

            Assert.False(p06get.IsAbstract);
            Assert.False(p06get.IsVirtual);
            Assert.False(p06get.IsMetadataVirtual());
            Assert.False(p06get.IsSealed);
            Assert.True(p06get.IsStatic);
            Assert.False(p06get.IsExtern);
            Assert.False(p06get.IsAsync);
            Assert.False(p06get.IsOverride);
            Assert.Equal(Accessibility.Public, p06get.DeclaredAccessibility);

            var p07 = i1.GetMember<PropertySymbol>("P07");
            var p07set = p07.SetMethod;

            Assert.False(p07.IsAbstract);
            Assert.True(p07.IsVirtual);
            Assert.False(p07.IsSealed);
            Assert.False(p07.IsStatic);
            Assert.False(p07.IsExtern);
            Assert.False(p07.IsOverride);
            Assert.Equal(Accessibility.Public, p07.DeclaredAccessibility);

            Assert.False(p07set.IsAbstract);
            Assert.True(p07set.IsVirtual);
            Assert.True(p07set.IsMetadataVirtual());
            Assert.False(p07set.IsSealed);
            Assert.False(p07set.IsStatic);
            Assert.False(p07set.IsExtern);
            Assert.False(p07set.IsAsync);
            Assert.False(p07set.IsOverride);
            Assert.Equal(Accessibility.Public, p07set.DeclaredAccessibility);

            var p08 = i1.GetMember<PropertySymbol>("P08");
            var p08get = p08.GetMethod;

            Assert.False(p08.IsAbstract);
            Assert.False(p08.IsVirtual);
            Assert.False(p08.IsSealed);
            Assert.False(p08.IsStatic);
            Assert.False(p08.IsExtern);
            Assert.False(p08.IsOverride);
            Assert.Equal(Accessibility.Public, p08.DeclaredAccessibility);

            Assert.False(p08get.IsAbstract);
            Assert.False(p08get.IsVirtual);
            Assert.False(p08get.IsMetadataVirtual());
            Assert.False(p08get.IsSealed);
            Assert.False(p08get.IsStatic);
            Assert.False(p08get.IsExtern);
            Assert.False(p08get.IsAsync);
            Assert.False(p08get.IsOverride);
            Assert.Equal(Accessibility.Public, p08get.DeclaredAccessibility);

            var p09 = i1.GetMember<PropertySymbol>("P09");
            var p09set = p09.SetMethod;

            Assert.True(p09.IsAbstract);
            Assert.False(p09.IsVirtual);
            Assert.False(p09.IsSealed);
            Assert.False(p09.IsStatic);
            Assert.False(p09.IsExtern);
            Assert.False(p09.IsOverride);
            Assert.Equal(Accessibility.Public, p09.DeclaredAccessibility);

            Assert.True(p09set.IsAbstract);
            Assert.False(p09set.IsVirtual);
            Assert.True(p09set.IsMetadataVirtual());
            Assert.False(p09set.IsSealed);
            Assert.False(p09set.IsStatic);
            Assert.False(p09set.IsExtern);
            Assert.False(p09set.IsAsync);
            Assert.False(p09set.IsOverride);
            Assert.Equal(Accessibility.Public, p09set.DeclaredAccessibility);

            var p10 = i1.GetMember<PropertySymbol>("P10");
            var p10get = p10.GetMethod;

            Assert.True(p10.IsAbstract);
            Assert.False(p10.IsVirtual);
            Assert.False(p10.IsSealed);
            Assert.False(p10.IsStatic);
            Assert.False(p10.IsExtern);
            Assert.False(p10.IsOverride);
            Assert.Equal(Accessibility.Public, p10.DeclaredAccessibility);

            Assert.True(p10get.IsAbstract);
            Assert.False(p10get.IsVirtual);
            Assert.True(p10get.IsMetadataVirtual());
            Assert.False(p10get.IsSealed);
            Assert.False(p10get.IsStatic);
            Assert.False(p10get.IsExtern);
            Assert.False(p10get.IsAsync);
            Assert.False(p10get.IsOverride);
            Assert.Equal(Accessibility.Public, p10get.DeclaredAccessibility);

            var p11 = i1.GetMember<PropertySymbol>("P11");

            Assert.False(p11.IsAbstract);
            Assert.True(p11.IsVirtual);
            Assert.False(p11.IsSealed);
            Assert.False(p11.IsStatic);
            Assert.True(p11.IsExtern);
            Assert.False(p11.IsOverride);
            Assert.Equal(Accessibility.Public, p11.DeclaredAccessibility);

            ValidateP11Accessor(p11.GetMethod);
            ValidateP11Accessor(p11.SetMethod);
            void ValidateP11Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.True(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.True(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p12 = i1.GetMember<PropertySymbol>("P12");

            Assert.True(p12.IsAbstract);
            Assert.False(p12.IsVirtual);
            Assert.False(p12.IsSealed);
            Assert.False(p12.IsStatic);
            Assert.False(p12.IsExtern);
            Assert.False(p12.IsOverride);
            Assert.Equal(Accessibility.Public, p12.DeclaredAccessibility);

            ValidateP12Accessor(p12.GetMethod);
            ValidateP12Accessor(p12.SetMethod);
            void ValidateP12Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p13 = i1.GetMember<PropertySymbol>("P13");

            Assert.True(p13.IsAbstract);
            Assert.False(p13.IsVirtual);
            Assert.False(p13.IsSealed);
            Assert.False(p13.IsStatic);
            Assert.False(p13.IsExtern);
            Assert.False(p13.IsOverride);
            Assert.Equal(Accessibility.Public, p13.DeclaredAccessibility);

            ValidateP13Accessor(p13.GetMethod);
            ValidateP13Accessor(p13.SetMethod);
            void ValidateP13Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p14 = i1.GetMember<PropertySymbol>("P14");

            Assert.True(p14.IsAbstract);
            Assert.False(p14.IsVirtual);
            Assert.False(p14.IsSealed);
            Assert.False(p14.IsStatic);
            Assert.False(p14.IsExtern);
            Assert.False(p14.IsOverride);
            Assert.Equal(Accessibility.Public, p14.DeclaredAccessibility);

            ValidateP14Accessor(p14.GetMethod);
            ValidateP14Accessor(p14.SetMethod);
            void ValidateP14Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p15 = i1.GetMember<PropertySymbol>("P15");

            Assert.True(p15.IsAbstract);
            Assert.False(p15.IsVirtual);
            Assert.False(p15.IsSealed);
            Assert.False(p15.IsStatic);
            Assert.False(p15.IsExtern);
            Assert.False(p15.IsOverride);
            Assert.Equal(Accessibility.Public, p15.DeclaredAccessibility);

            ValidateP15Accessor(p15.GetMethod, Accessibility.Public);
            ValidateP15Accessor(p15.SetMethod, Accessibility.Internal);
            void ValidateP15Accessor(MethodSymbol accessor, Accessibility accessibility)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(accessibility, accessor.DeclaredAccessibility);
            }

            var p16 = i1.GetMember<PropertySymbol>("P16");

            Assert.True(p16.IsAbstract);
            Assert.False(p16.IsVirtual);
            Assert.False(p16.IsSealed);
            Assert.False(p16.IsStatic);
            Assert.False(p16.IsExtern);
            Assert.False(p16.IsOverride);
            Assert.Equal(Accessibility.Public, p16.DeclaredAccessibility);

            ValidateP16Accessor(p16.GetMethod, Accessibility.Private);
            ValidateP16Accessor(p16.SetMethod, Accessibility.Public);
            void ValidateP16Accessor(MethodSymbol accessor, Accessibility accessibility)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(accessibility, accessor.DeclaredAccessibility);
            }

            var p17 = i1.GetMember<PropertySymbol>("P17");
            var p17get = p17.GetMethod;

            Assert.True(p17.IsAbstract);
            Assert.False(p17.IsVirtual);
            Assert.False(p17.IsSealed);
            Assert.False(p17.IsStatic);
            Assert.False(p17.IsExtern);
            Assert.False(p17.IsOverride);
            Assert.Equal(Accessibility.Public, p17.DeclaredAccessibility);

            Assert.True(p17get.IsAbstract);
            Assert.False(p17get.IsVirtual);
            Assert.True(p17get.IsMetadataVirtual());
            Assert.False(p17get.IsSealed);
            Assert.False(p17get.IsStatic);
            Assert.False(p17get.IsExtern);
            Assert.False(p17get.IsAsync);
            Assert.False(p17get.IsOverride);
            Assert.Equal(Accessibility.Private, p17get.DeclaredAccessibility);
        }

        [Fact]
        public void PropertyModifiers_02()
        {
            var source1 =
@"
public interface I1
{
    public int P01 {get; set;}
    protected int P02 {get;}
    protected internal int P03 {set;}
    internal int P04 {get;}
    private int P05 {set;}
    static int P06 {get;}
    virtual int P07 {set;}
    sealed int P08 {get;}
    override int P09 {set;}
    abstract int P10 {get;}
    extern int P11 {get; set;}

    int P12 { public get; set;}
    int P13 { get; protected set;}
    int P14 { protected internal get; set;}
    int P15 { get; internal set;}
    int P16 { private get; set;}
    int P17 { private get;}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,16): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public int P01 {get; set;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P01").WithArguments("public", "7", "7.1").WithLocation(4, 16),
                // (5,19): error CS0106: The modifier 'protected' is not valid for this item
                //     protected int P02 {get;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P02").WithArguments("protected").WithLocation(5, 19),
                // (6,28): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal int P03 {set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P03").WithArguments("protected internal").WithLocation(6, 28),
                // (7,18): error CS8503: The modifier 'internal' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     internal int P04 {get;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P04").WithArguments("internal", "7", "7.1").WithLocation(7, 18),
                // (8,17): error CS8503: The modifier 'private' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     private int P05 {set;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P05").WithArguments("private", "7", "7.1").WithLocation(8, 17),
                // (8,22): error CS0501: 'I1.P05.set' must declare a body because it is not marked abstract, extern, or partial
                //     private int P05 {set;}
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I1.P05.set").WithLocation(8, 22),
                // (9,16): error CS8503: The modifier 'static' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     static int P06 {get;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P06").WithArguments("static", "7", "7.1").WithLocation(9, 16),
                // (9,21): error CS0501: 'I1.P06.get' must declare a body because it is not marked abstract, extern, or partial
                //     static int P06 {get;}
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P06.get").WithLocation(9, 21),
                // (10,17): error CS8503: The modifier 'virtual' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     virtual int P07 {set;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P07").WithArguments("virtual", "7", "7.1").WithLocation(10, 17),
                // (10,22): error CS0501: 'I1.P07.set' must declare a body because it is not marked abstract, extern, or partial
                //     virtual int P07 {set;}
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I1.P07.set").WithLocation(10, 22),
                // (11,16): error CS8503: The modifier 'sealed' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     sealed int P08 {get;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P08").WithArguments("sealed", "7", "7.1").WithLocation(11, 16),
                // (11,21): error CS0501: 'I1.P08.get' must declare a body because it is not marked abstract, extern, or partial
                //     sealed int P08 {get;}
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P08.get").WithLocation(11, 21),
                // (12,18): error CS0106: The modifier 'override' is not valid for this item
                //     override int P09 {set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P09").WithArguments("override").WithLocation(12, 18),
                // (13,18): error CS8503: The modifier 'abstract' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     abstract int P10 {get;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P10").WithArguments("abstract", "7", "7.1").WithLocation(13, 18),
                // (14,16): error CS8503: The modifier 'extern' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     extern int P11 {get; set;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P11").WithArguments("extern", "7", "7.1").WithLocation(14, 16),
                // (16,22): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     int P12 { public get; set;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "get").WithArguments("public", "7", "7.1").WithLocation(16, 22),
                // (16,22): error CS0273: The accessibility modifier of the 'I1.P12.get' accessor must be more restrictive than the property or indexer 'I1.P12'
                //     int P12 { public get; set;}
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("I1.P12.get", "I1.P12").WithLocation(16, 22),
                // (17,30): error CS0106: The modifier 'protected' is not valid for this item
                //     int P13 { get; protected set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("protected").WithLocation(17, 30),
                // (18,34): error CS0106: The modifier 'protected internal' is not valid for this item
                //     int P14 { protected internal get; set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("protected internal").WithLocation(18, 34),
                // (19,29): error CS8503: The modifier 'internal' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     int P15 { get; internal set;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "set").WithArguments("internal", "7", "7.1").WithLocation(19, 29),
                // (20,23): error CS8503: The modifier 'private' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     int P16 { private get; set;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "get").WithArguments("private", "7", "7.1").WithLocation(20, 23),
                // (20,23): error CS0442: 'I1.P16.get': abstract properties cannot have private accessors
                //     int P16 { private get; set;}
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "get").WithArguments("I1.P16.get").WithLocation(20, 23),
                // (21,23): error CS8503: The modifier 'private' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     int P17 { private get;}
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "get").WithArguments("private", "7", "7.1").WithLocation(21, 23),
                // (21,9): error CS0276: 'I1.P17': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     int P17 { private get;}
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "P17").WithArguments("I1.P17").WithLocation(21, 9),
                // (14,21): warning CS0626: Method, operator, or accessor 'I1.P11.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern int P11 {get; set;}
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("I1.P11.get").WithLocation(14, 21),
                // (14,26): warning CS0626: Method, operator, or accessor 'I1.P11.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern int P11 {get; set;}
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("I1.P11.set").WithLocation(14, 26)
                );

            ValidateSymbolsPropertyModifiers_01(compilation1);
        }

        [Fact]
        public void PropertyModifiers_03()
        {
            ValidatePropertyImplementation_101(@"
public interface I1
{
    public virtual int P1 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
    }
}

class Test1 : I1
{}
");

            ValidatePropertyImplementation_101(@"
public interface I1
{
    public virtual int P1 
    {
        get => Test1.GetP1();
    }
}

class Test1 : I1
{
    public static int GetP1()
    {
        System.Console.WriteLine(""get P1"");
        return 0;
    }
}
");

            ValidatePropertyImplementation_101(@"
public interface I1
{
    public virtual int P1 => Test1.GetP1();
}

class Test1 : I1
{
    public static int GetP1()
    {
        System.Console.WriteLine(""get P1"");
        return 0;
    }
}
");

            ValidatePropertyImplementation_102(@"
public interface I1
{
    public virtual int P1 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
");

            ValidatePropertyImplementation_102(@"
public interface I1
{
    public virtual int P1 
    {
        get => Test1.GetP1();
        set => System.Console.WriteLine(""set P1"");
    }
}

class Test1 : I1
{
    public static int GetP1()
    {
        System.Console.WriteLine(""get P1"");
        return 0;
    }
}
");

            ValidatePropertyImplementation_103(@"
public interface I1
{
    public virtual int P1 
    {
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
");

            ValidatePropertyImplementation_103(@"
public interface I1
{
    public virtual int P1 
    {
        set => System.Console.WriteLine(""set P1"");
    }
}

class Test1 : I1
{}
");
        }

        [Fact]
        public void PropertyModifiers_04()
        {
            var source1 =
@"
public interface I1
{
    public virtual int P1 { get; } = 0; 
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,24): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     public virtual int P1 { get; } = 0; 
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P1").WithArguments("I1.P1").WithLocation(4, 24),
                // (4,29): error CS0501: 'I1.P1.get' must declare a body because it is not marked abstract, extern, or partial
                //     public virtual int P1 { get; } = 0; 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P1.get").WithLocation(4, 29)
                );

            ValidatePropertyModifiers_04(compilation1, "P1");
        }

        private static void ValidatePropertyModifiers_04(CSharpCompilation compilation1, string propertyName)
        {
            var i1 = compilation1.GlobalNamespace.GetTypeMember("I1");
            var p1 = i1.GetMember<PropertySymbol>(propertyName);
            var p1get = p1.GetMethod;

            Assert.False(p1.IsAbstract);
            Assert.True(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);

            Assert.False(p1get.IsAbstract);
            Assert.True(p1get.IsVirtual);
            Assert.True(p1get.IsMetadataVirtual());
            Assert.False(p1get.IsSealed);
            Assert.False(p1get.IsStatic);
            Assert.False(p1get.IsExtern);
            Assert.False(p1get.IsAsync);
            Assert.False(p1get.IsOverride);
            Assert.Equal(Accessibility.Public, p1get.DeclaredAccessibility);
        }

        [Fact]
        public void PropertyModifiers_05()
        {
            var source1 =
@"
public interface I1
{
    public abstract int P1 {get; set;} 
}
public interface I2
{
    int P2 {get; set;} 
}

class Test1 : I1
{
    public int P1 
    {
        get
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set => System.Console.WriteLine(""set_P1"");
    }
}
class Test2 : I2
{
    public int P2 
    {
        get
        {
            System.Console.WriteLine(""get_P2"");
            return 0;
        }
        set => System.Console.WriteLine(""set_P2"");
    }

    static void Main()
    {
        I1 x = new Test1();
        x.P1 = x.P1;
        I2 y = new Test2();
        y.P2 = y.P2;
    }
}
";

            ValidatePropertyModifiers_05(source1);
        }

        private void ValidatePropertyModifiers_05(string source1)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            CompileAndVerify(compilation1, expectedOutput:
@"get_P1
set_P1
get_P2
set_P2", symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                for (int i = 1; i <= 2; i++)
                {
                    var test1 = m.GlobalNamespace.GetTypeMember("Test" + i);
                    var i1 = m.GlobalNamespace.GetTypeMember("I" + i);
                    var p1 = GetSingleProperty(i1);
                    var test1P1 = GetSingleProperty(test1);

                    Assert.True(p1.IsAbstract);
                    Assert.False(p1.IsVirtual);
                    Assert.False(p1.IsSealed);
                    Assert.False(p1.IsStatic);
                    Assert.False(p1.IsExtern);
                    Assert.False(p1.IsOverride);
                    Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
                    Assert.Same(test1P1, test1.FindImplementationForInterfaceMember(p1));

                    ValidateAccessor(p1.GetMethod, test1P1.GetMethod);
                    ValidateAccessor(p1.SetMethod, test1P1.SetMethod);

                    void ValidateAccessor(MethodSymbol accessor, MethodSymbol implementation)
                    {
                        Assert.True(accessor.IsAbstract);
                        Assert.False(accessor.IsVirtual);
                        Assert.True(accessor.IsMetadataVirtual());
                        Assert.False(accessor.IsSealed);
                        Assert.False(accessor.IsStatic);
                        Assert.False(accessor.IsExtern);
                        Assert.False(accessor.IsAsync);
                        Assert.False(accessor.IsOverride);
                        Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                        Assert.Same(implementation, test1.FindImplementationForInterfaceMember(accessor));
                    }
                }
            }
        }

        private static PropertySymbol GetSingleProperty(NamedTypeSymbol container)
        {
            return container.GetMembers().OfType<PropertySymbol>().Single();
        }

        private static PropertySymbol GetSingleProperty(CSharpCompilation compilation, string containerName)
        {
            return GetSingleProperty(compilation.GetTypeByMetadataName(containerName));
        }

        private static PropertySymbol GetSingleProperty(ModuleSymbol m, string containerName)
        {
            return GetSingleProperty(m.GlobalNamespace.GetTypeMember(containerName));
        }

        [Fact]
        public void PropertyModifiers_06()
        {
            var source1 =
@"
public interface I1
{
    public abstract int P1 {get; set;} 
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,25): error CS8503: The modifier 'abstract' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public abstract int P1 {get; set;} 
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P1").WithArguments("abstract", "7", "7.1").WithLocation(4, 25),
                // (4,25): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public abstract int P1 {get; set;} 
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P1").WithArguments("public", "7", "7.1").WithLocation(4, 25)
                );

            ValidatePropertyModifiers_06(compilation1, "P1");
        }

        private static void ValidatePropertyModifiers_06(CSharpCompilation compilation1, string propertyName)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p1 = i1.GetMember<PropertySymbol>(propertyName);

            Assert.True(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);

            ValidateAccessor(p1.GetMethod);
            ValidateAccessor(p1.SetMethod);

            void ValidateAccessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }
        }

        [Fact]
        public void PropertyModifiers_07()
        {
            var source1 =
@"
public interface I1
{
    public static int P1 
    {
        get
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set 
        {
            System.Console.WriteLine(""set_P1"");
        }
    }

    internal static int P2 
    {
        get
        {
            System.Console.WriteLine(""get_P2"");
            return P3;
        }
        set
        {
            System.Console.WriteLine(""set_P2"");
            P3 = value;
        }
    }

    private static int P3 
    {
        get => Test1.GetP3();
        set => System.Console.WriteLine(""set_P3"");
    }

    internal static int P4 => Test1.GetP4();

    internal static int P5 
    {
        get
        {
            System.Console.WriteLine(""get_P5"");
            return 0;
        }
    }

    internal static int P6 
    {
        get => Test1.GetP6();
    }

    internal static int P7 
    {
        set
        {
            System.Console.WriteLine(""set_P7"");
        }
    }

    internal static int P8 
    {
        set => System.Console.WriteLine(""set_P8"");
    }
}

class Test1 : I1
{
    static void Main()
    {
        I1.P1 = I1.P1;
        I1.P2 = I1.P2;
        var x = I1.P4;
        x = I1.P5;
        x = I1.P6;
        I1.P7 = x;
        I1.P8 = x;
    }

    public static int GetP3()
    {
        System.Console.WriteLine(""get_P3"");
        return 0;
    }

    public static int GetP4()
    {
        System.Console.WriteLine(""get_P4"");
        return 0;
    }

    public static int GetP6()
    {
        System.Console.WriteLine(""get_P6"");
        return 0;
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, expectedOutput:
@"get_P1
set_P1
get_P2
get_P3
set_P2
set_P3
get_P4
get_P5
get_P6
set_P7
set_P8", symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = m.GlobalNamespace.GetTypeMember("I1");

                foreach (var tuple in new[] { (name: "P1", access: Accessibility.Public),
                                              (name: "P2", access: Accessibility.Internal),
                                              (name: "P3", access: Accessibility.Private),
                                              (name: "P4", access: Accessibility.Internal),
                                              (name: "P5", access: Accessibility.Internal),
                                              (name: "P6", access: Accessibility.Internal),
                                              (name: "P7", access: Accessibility.Internal),
                                              (name: "P8", access: Accessibility.Internal)})
                {
                    var p1 = i1.GetMember<PropertySymbol>(tuple.name);

                    Assert.False(p1.IsAbstract);
                    Assert.False(p1.IsVirtual);
                    Assert.False(p1.IsSealed);
                    Assert.True(p1.IsStatic);
                    Assert.False(p1.IsExtern);
                    Assert.False(p1.IsOverride);
                    Assert.Equal(tuple.access, p1.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(p1));

                    switch (tuple.name)
                    {
                        case "P7":
                        case "P8":
                            Assert.Null(p1.GetMethod);
                            ValidateAccessor(p1.SetMethod);
                            break;
                        case "P4":
                        case "P5":
                        case "P6":
                            Assert.Null(p1.SetMethod);
                            ValidateAccessor(p1.GetMethod);
                            break;
                        default:
                            ValidateAccessor(p1.GetMethod);
                            ValidateAccessor(p1.SetMethod);
                            break;
                    }

                    void ValidateAccessor(MethodSymbol accessor)
                    {
                        Assert.False(accessor.IsAbstract);
                        Assert.False(accessor.IsVirtual);
                        Assert.False(accessor.IsMetadataVirtual());
                        Assert.False(accessor.IsSealed);
                        Assert.True(accessor.IsStatic);
                        Assert.False(accessor.IsExtern);
                        Assert.False(accessor.IsAsync);
                        Assert.False(accessor.IsOverride);
                        Assert.Equal(tuple.access, accessor.DeclaredAccessibility);
                        Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                    }
                }
            }
        }

        [Fact]
        public void PropertyModifiers_08()
        {
            var source1 =
@"
public interface I1
{
    abstract static int P1 {get;} 

    virtual static int P2 {set {}} 
    
    sealed static int P3 => 0; 

    static int P4 {get;} = 0;  
}

class Test1 : I1
{
    int I1.P1 => 0;
    int I1.P2 {set {}}
    int I1.P3 => 0;
    int I1.P4 {set {}}
}

class Test2 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,25): error CS0112: A static member 'I1.P1' cannot be marked as override, virtual, or abstract
                //     abstract static int P1 {get;} 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "P1").WithArguments("I1.P1").WithLocation(4, 25),
                // (6,24): error CS0112: A static member 'I1.P2' cannot be marked as override, virtual, or abstract
                //     virtual static int P2 {set {}} 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "P2").WithArguments("I1.P2").WithLocation(6, 24),
                // (8,23): error CS0238: 'I1.P3' cannot be sealed because it is not an override
                //     sealed static int P3 => 0; 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P3").WithArguments("I1.P3").WithLocation(8, 23),
                // (10,16): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     static int P4 {get;} = 0;  
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P4").WithArguments("I1.P4").WithLocation(10, 16),
                // (10,20): error CS0501: 'I1.P4.get' must declare a body because it is not marked abstract, extern, or partial
                //     static int P4 {get;} = 0;  
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P4.get").WithLocation(10, 20),
                // (15,12): error CS0539: 'Test1.P1' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I1.P1 => 0;
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P1").WithArguments("Test1.P1").WithLocation(15, 12),
                // (16,12): error CS0539: 'Test1.P2' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I1.P2 {set {}}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P2").WithArguments("Test1.P2").WithLocation(16, 12),
                // (17,12): error CS0539: 'Test1.P3' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I1.P3 => 0;
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P3").WithArguments("Test1.P3").WithLocation(17, 12),
                // (18,12): error CS0539: 'Test1.P4' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I1.P4 {set {}}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P4").WithArguments("Test1.P4").WithLocation(18, 12)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p1 = i1.GetMember<PropertySymbol>("P1");
            var p1get = p1.GetMethod;

            Assert.True(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.True(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));

            Assert.True(p1get.IsAbstract);
            Assert.False(p1get.IsVirtual);
            Assert.True(p1get.IsMetadataVirtual());
            Assert.False(p1get.IsSealed);
            Assert.True(p1get.IsStatic);
            Assert.False(p1get.IsExtern);
            Assert.False(p1get.IsAsync);
            Assert.False(p1get.IsOverride);
            Assert.Equal(Accessibility.Public, p1get.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1get));

            var p2 = i1.GetMember<PropertySymbol>("P2");
            var p2set = p2.SetMethod;

            Assert.False(p2.IsAbstract);
            Assert.True(p2.IsVirtual);
            Assert.False(p2.IsSealed);
            Assert.True(p2.IsStatic);
            Assert.False(p2.IsExtern);
            Assert.False(p2.IsOverride);
            Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p2));

            Assert.False(p2set.IsAbstract);
            Assert.True(p2set.IsVirtual);
            Assert.True(p2set.IsMetadataVirtual());
            Assert.False(p2set.IsSealed);
            Assert.True(p2set.IsStatic);
            Assert.False(p2set.IsExtern);
            Assert.False(p2set.IsAsync);
            Assert.False(p2set.IsOverride);
            Assert.Equal(Accessibility.Public, p2set.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p2set));

            var p3 = i1.GetMember<PropertySymbol>("P3");
            var p3get = p3.GetMethod;

            Assert.False(p3.IsAbstract);
            Assert.False(p3.IsVirtual);
            Assert.True(p3.IsSealed);
            Assert.True(p3.IsStatic);
            Assert.False(p3.IsExtern);
            Assert.False(p3.IsOverride);
            Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p3));

            Assert.False(p3get.IsAbstract);
            Assert.False(p3get.IsVirtual);
            Assert.False(p3get.IsMetadataVirtual());
            Assert.True(p3get.IsSealed);
            Assert.True(p3get.IsStatic);
            Assert.False(p3get.IsExtern);
            Assert.False(p3get.IsAsync);
            Assert.False(p3get.IsOverride);
            Assert.Equal(Accessibility.Public, p3get.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p3get));
        }

        [Fact]
        public void PropertyModifiers_09()
        {
            var source1 =
@"
public interface I1
{
    private int P1
    {
        get
        { 
            System.Console.WriteLine(""get_P1"");
            return 0;
        }           
    }
    sealed void M()
    {
        var x = P1;
    }
}
public interface I2
{
    private int P2 
    {
        get
        { 
            System.Console.WriteLine(""get_P2"");
            return 0;
        }           
        set
        { 
            System.Console.WriteLine(""set_P2"");
        }           
    }
    sealed void M()
    {
        P2 = P2;
    }
}
public interface I3
{
    private int P3 
    {
        set
        { 
            System.Console.WriteLine(""set_P3"");
        }           
    }
    sealed void M()
    {
        P3 = 0;
    }
}
public interface I4
{
    private int P4 
    {
        get => GetP4();
    }

    private int GetP4()
    { 
        System.Console.WriteLine(""get_P4"");
        return 0;
    }           
    sealed void M()
    {
        var x = P4;
    }
}
public interface I5
{
    private int P5 
    {
        get => GetP5();
        set => System.Console.WriteLine(""set_P5"");
    }

    private int GetP5()
    { 
        System.Console.WriteLine(""get_P5"");
        return 0;
    }           
    sealed void M()
    {
        P5 = P5;
    }
}
public interface I6
{
    private int P6 
    {
        set => System.Console.WriteLine(""set_P6"");
    }
    sealed void M()
    {
        P6 = 0;
    }
}
public interface I7
{
    private int P7 => GetP7();

    private int GetP7()
    { 
        System.Console.WriteLine(""get_P7"");
        return 0;
    }           
    sealed void M()
    {
        var x = P7;
    }
}

class Test1 : I1, I2, I3, I4, I5, I6, I7
{
    static void Main()
    {
        I1 x1 = new Test1();
        x1.M();
        I2 x2 = new Test1();
        x2.M();
        I3 x3 = new Test1();
        x3.M();
        I4 x4 = new Test1();
        x4.M();
        I5 x5 = new Test1();
        x5.M();
        I6 x6 = new Test1();
        x6.M();
        I7 x7 = new Test1();
        x7.M();
    }
}
";

            ValidatePropertyModifiers_09(source1);
        }

        private void ValidatePropertyModifiers_09(string source1)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");

                for (int i = 1; i <= 7; i++)
                {
                    var i1 = m.GlobalNamespace.GetTypeMember("I" + i);
                    var p1 = GetSingleProperty(i1);

                    Assert.False(p1.IsAbstract);
                    Assert.False(p1.IsVirtual);
                    Assert.False(p1.IsSealed);
                    Assert.False(p1.IsStatic);
                    Assert.False(p1.IsExtern);
                    Assert.False(p1.IsOverride);
                    Assert.Equal(Accessibility.Private, p1.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(p1));

                    switch (i)
                    {
                        case 3:
                        case 6:
                            Assert.Null(p1.GetMethod);
                            ValidateAccessor(p1.SetMethod);
                            break;
                        case 1:
                        case 4:
                        case 7:
                            Assert.Null(p1.SetMethod);
                            ValidateAccessor(p1.GetMethod);
                            break;
                        default:
                            ValidateAccessor(p1.GetMethod);
                            ValidateAccessor(p1.SetMethod);
                            break;
                    }

                    void ValidateAccessor(MethodSymbol acessor)
                    {
                        Assert.False(acessor.IsAbstract);
                        Assert.False(acessor.IsVirtual);
                        Assert.False(acessor.IsMetadataVirtual());
                        Assert.False(acessor.IsSealed);
                        Assert.False(acessor.IsStatic);
                        Assert.False(acessor.IsExtern);
                        Assert.False(acessor.IsAsync);
                        Assert.False(acessor.IsOverride);
                        Assert.Equal(Accessibility.Private, acessor.DeclaredAccessibility);
                        Assert.Null(test1.FindImplementationForInterfaceMember(acessor));
                    }
                }
            }
        }

        [Fact]
        public void PropertyModifiers_10()
        {
            var source1 =
@"
public interface I1
{
    abstract private int P1 { get; } 

    virtual private int P2 => 0;

    sealed private int P3 
    {
        get => 0;
        set {}
    }

    private int P4 {get;} = 0;
}

class Test1 : I1
{
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,26): error CS0621: 'I1.P1': virtual or abstract members cannot be private
                //     abstract private int P1 { get; } 
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "P1").WithArguments("I1.P1").WithLocation(4, 26),
                // (6,25): error CS0621: 'I1.P2': virtual or abstract members cannot be private
                //     virtual private int P2 => 0;
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "P2").WithArguments("I1.P2").WithLocation(6, 25),
                // (8,24): error CS0238: 'I1.P3' cannot be sealed because it is not an override
                //     sealed private int P3 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P3").WithArguments("I1.P3").WithLocation(8, 24),
                // (14,17): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     private int P4 {get;} = 0;
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P4").WithArguments("I1.P4").WithLocation(14, 17),
                // (14,21): error CS0501: 'I1.P4.get' must declare a body because it is not marked abstract, extern, or partial
                //     private int P4 {get;} = 0;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P4.get").WithLocation(14, 21),
                // (17,15): error CS0535: 'Test1' does not implement interface member 'I1.P1'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.P1").WithLocation(17, 15)
                );

            ValidatePropertyModifiers_10(compilation1);
        }

        private static void ValidatePropertyModifiers_10(CSharpCompilation compilation1)
        {
            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p1 = i1.GetMembers().OfType<PropertySymbol>().ElementAt(0);
            var p1get = p1.GetMethod;

            Assert.True(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Private, p1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));

            Assert.True(p1get.IsAbstract);
            Assert.False(p1get.IsVirtual);
            Assert.True(p1get.IsMetadataVirtual());
            Assert.False(p1get.IsSealed);
            Assert.False(p1get.IsStatic);
            Assert.False(p1get.IsExtern);
            Assert.False(p1get.IsAsync);
            Assert.False(p1get.IsOverride);
            Assert.Equal(Accessibility.Private, p1get.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1get));

            var p2 = i1.GetMembers().OfType<PropertySymbol>().ElementAt(1);
            var p2get = p2.GetMethod;

            Assert.False(p2.IsAbstract);
            Assert.True(p2.IsVirtual);
            Assert.False(p2.IsSealed);
            Assert.False(p2.IsStatic);
            Assert.False(p2.IsExtern);
            Assert.False(p2.IsOverride);
            Assert.Equal(Accessibility.Private, p2.DeclaredAccessibility);
            Assert.Same(p2, test1.FindImplementationForInterfaceMember(p2));

            Assert.False(p2get.IsAbstract);
            Assert.True(p2get.IsVirtual);
            Assert.True(p2get.IsMetadataVirtual());
            Assert.False(p2get.IsSealed);
            Assert.False(p2get.IsStatic);
            Assert.False(p2get.IsExtern);
            Assert.False(p2get.IsAsync);
            Assert.False(p2get.IsOverride);
            Assert.Equal(Accessibility.Private, p2get.DeclaredAccessibility);
            Assert.Same(p2get, test1.FindImplementationForInterfaceMember(p2get));

            var p3 = i1.GetMembers().OfType<PropertySymbol>().ElementAt(2);

            Assert.False(p3.IsAbstract);
            Assert.False(p3.IsVirtual);
            Assert.True(p3.IsSealed);
            Assert.False(p3.IsStatic);
            Assert.False(p3.IsExtern);
            Assert.False(p3.IsOverride);
            Assert.Equal(Accessibility.Private, p3.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p3));

            ValidateP3Accessor(p3.GetMethod);
            ValidateP3Accessor(p3.SetMethod);
            void ValidateP3Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.False(accessor.IsMetadataVirtual());
                Assert.True(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Private, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
            }

            var p4 = i1.GetMembers().OfType<PropertySymbol>().ElementAt(3);
            var p4get = p4.GetMethod;

            Assert.False(p4.IsAbstract);
            Assert.False(p4.IsVirtual);
            Assert.False(p4.IsSealed);
            Assert.False(p4.IsStatic);
            Assert.False(p4.IsExtern);
            Assert.False(p4.IsOverride);
            Assert.Equal(Accessibility.Private, p4.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p4));

            Assert.False(p4get.IsAbstract);
            Assert.False(p4get.IsVirtual);
            Assert.False(p4get.IsMetadataVirtual());
            Assert.False(p4get.IsSealed);
            Assert.False(p4get.IsStatic);
            Assert.False(p4get.IsExtern);
            Assert.False(p4get.IsAsync);
            Assert.False(p4get.IsOverride);
            Assert.Equal(Accessibility.Private, p4get.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p4get));
        }

        [Fact]
        public void PropertyModifiers_11()
        {
            var source1 =
@"
public interface I1
{
    internal abstract int P1 {get; set;} 

    sealed void Test()
    {
        P1 = P1;
    }
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.Test();
    }

    public int P1 
    {
        get
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""set_P1"");
        }
    }
}
";

            ValidatePropertyModifiers_11(source1, source2,
                // (2,15): error CS0535: 'Test2' does not implement interface member 'I1.P1'
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.P1").WithLocation(2, 15)
                );
        }

        private void ValidatePropertyModifiers_11(string source1, string source2, params DiagnosticDescription[] expected)
        { 
            var compilation1 = CreateStandardCompilation(source1 + source2, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            Validate1(compilation1.SourceModule);

            void Validate1(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = test1.Interfaces.Single();
                var p1 = GetSingleProperty(i1);
                var test1P1 = GetSingleProperty(test1);
                var p1get = p1.GetMethod;
                var p1set = p1.SetMethod;

                ValidateProperty(p1);
                ValidateMethod(p1get);
                ValidateMethod(p1set);
                Assert.Same(test1P1, test1.FindImplementationForInterfaceMember(p1));
                Assert.Same(test1P1.GetMethod, test1.FindImplementationForInterfaceMember(p1get));
                Assert.Same(test1P1.SetMethod, test1.FindImplementationForInterfaceMember(p1set));
            }

            void ValidateProperty(PropertySymbol p1)
            {
                Assert.True(p1.IsAbstract);
                Assert.False(p1.IsVirtual);
                Assert.False(p1.IsSealed);
                Assert.False(p1.IsStatic);
                Assert.False(p1.IsExtern);
                Assert.False(p1.IsOverride);
                Assert.Equal(Accessibility.Internal, p1.DeclaredAccessibility);
            }

            void ValidateMethod(MethodSymbol m1)
            {
                Assert.True(m1.IsAbstract);
                Assert.False(m1.IsVirtual);
                Assert.True(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(Accessibility.Internal, m1.DeclaredAccessibility);
            }

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            {
                var i1 = compilation2.GetTypeByMetadataName("I1");
                var p1 = GetSingleProperty(i1);
                var p1get = p1.GetMethod;
                var p1set = p1.SetMethod;

                ValidateProperty(p1);
                ValidateMethod(p1get);
                ValidateMethod(p1set);
            }

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation2.ToMetadataReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate1);

            Validate1(compilation3.SourceModule);

            var compilation4 = CreateStandardCompilation(source2, new[] { compilation2.EmitToImageReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation4.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation4, verify: false, symbolValidator: Validate1);

            Validate1(compilation4.SourceModule);

            var source3 =
@"
class Test2 : I1
{
}
";

            var compilation5 = CreateStandardCompilation(source3, new[] { compilation2.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation5.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation5.VerifyDiagnostics(expected);

            {
                var test2 = compilation5.GetTypeByMetadataName("Test2");
                var i1 = compilation5.GetTypeByMetadataName("I1");
                var p1 = GetSingleProperty(i1);
                Assert.Null(test2.FindImplementationForInterfaceMember(p1));
                Assert.Null(test2.FindImplementationForInterfaceMember(p1.GetMethod));
                Assert.Null(test2.FindImplementationForInterfaceMember(p1.SetMethod));
            }

            var compilation6 = CreateStandardCompilation(source3, new[] { compilation2.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation6.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation6.VerifyDiagnostics(expected);

            {
                var test2 = compilation6.GetTypeByMetadataName("Test2");
                var i1 = compilation6.GetTypeByMetadataName("I1");
                var p1 = GetSingleProperty(i1);
                Assert.Null(test2.FindImplementationForInterfaceMember(p1));
                Assert.Null(test2.FindImplementationForInterfaceMember(p1.GetMethod));
                Assert.Null(test2.FindImplementationForInterfaceMember(p1.SetMethod));
            }
        }

        [Fact]
        public void PropertyModifiers_12()
        {
            var source1 =
@"
public interface I1
{
    internal abstract int P1 {get; set;} 
}

class Test1 : I1
{
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (7,15): error CS0535: 'Test1' does not implement interface member 'I1.P1'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.P1").WithLocation(7, 15)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p1 = i1.GetMember<PropertySymbol>("P1");
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));
            Assert.Null(test1.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Null(test1.FindImplementationForInterfaceMember(p1.SetMethod));
        }

        [Fact]
        public void PropertyModifiers_13()
        {
            var source1 =
@"
public interface I1
{
    public sealed int P1
    {
        get
        { 
            System.Console.WriteLine(""get_P1"");
            return 0;
        }           
    }
}
public interface I2
{
    public sealed int P2 
    {
        get
        { 
            System.Console.WriteLine(""get_P2"");
            return 0;
        }           
        set
        { 
            System.Console.WriteLine(""set_P2"");
        }           
    }
}
public interface I3
{
    public sealed int P3 
    {
        set
        { 
            System.Console.WriteLine(""set_P3"");
        }           
    }
}
public interface I4
{
    public sealed int P4 
    {
        get => GetP4();
    }

    private int GetP4()
    { 
        System.Console.WriteLine(""get_P4"");
        return 0;
    }           
}
public interface I5
{
    public sealed int P5 
    {
        get => GetP5();
        set => System.Console.WriteLine(""set_P5"");
    }

    private int GetP5()
    { 
        System.Console.WriteLine(""get_P5"");
        return 0;
    }           
}
public interface I6
{
    public sealed int P6 
    {
        set => System.Console.WriteLine(""set_P6"");
    }
}
public interface I7
{
    public sealed int P7 => GetP7();

    private int GetP7()
    { 
        System.Console.WriteLine(""get_P7"");
        return 0;
    }           
}

class Test1 : I1
{
    static void Main()
    {
        I1 i1 = new Test1();
        var x = i1.P1;
        I2 i2 = new Test2();
        i2.P2 = i2.P2;
        I3 i3 = new Test3();
        i3.P3 = x;
        I4 i4 = new Test4();
        x = i4.P4;
        I5 i5 = new Test5();
        i5.P5 = i5.P5;
        I6 i6 = new Test6();
        i6.P6 = x;
        I7 i7 = new Test7();
        x = i7.P7;
    }

    public int P1 => throw null;
}
class Test2 : I2
{
    public int P2 
    {
        get => throw null;          
        set => throw null;         
    }
}
class Test3 : I3
{
    public int P3 
    {
        set => throw null;      
    }
}
class Test4 : I4
{
    public int P4 
    {
        get => throw null;
    }
}
class Test5 : I5
{
    public int P5 
    {
        get => throw null;
        set => throw null;
    }
}
class Test6 : I6
{
    public int P6 
    {
        set => throw null;
    }
}
class Test7 : I7
{
    public int P7 => throw null;
}
";

            ValidatePropertyModifiers_13(source1);
        }

        private void ValidatePropertyModifiers_13(string source1)
        { 
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate(ModuleSymbol m)
            {
                for (int i = 1; i <= 7; i++)
                {
                    var test1 = m.GlobalNamespace.GetTypeMember("Test" + i);
                    var i1 = m.GlobalNamespace.GetTypeMember("I" + i);
                    var p1 = GetSingleProperty(i1);

                    Assert.False(p1.IsAbstract);
                    Assert.False(p1.IsVirtual);
                    Assert.False(p1.IsSealed);
                    Assert.False(p1.IsStatic);
                    Assert.False(p1.IsExtern);
                    Assert.False(p1.IsOverride);
                    Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(p1));

                    switch (i)
                    {
                        case 3:
                        case 6:
                            Assert.Null(p1.GetMethod);
                            ValidateAccessor(p1.SetMethod);
                            break;
                        case 1:
                        case 4:
                        case 7:
                            Assert.Null(p1.SetMethod);
                            ValidateAccessor(p1.GetMethod);
                            break;
                        default:
                            ValidateAccessor(p1.GetMethod);
                            ValidateAccessor(p1.SetMethod);
                            break;
                    }

                    void ValidateAccessor(MethodSymbol acessor)
                    {
                        Assert.False(acessor.IsAbstract);
                        Assert.False(acessor.IsVirtual);
                        Assert.False(acessor.IsMetadataVirtual());
                        Assert.False(acessor.IsSealed);
                        Assert.False(acessor.IsStatic);
                        Assert.False(acessor.IsExtern);
                        Assert.False(acessor.IsAsync);
                        Assert.False(acessor.IsOverride);
                        Assert.Equal(Accessibility.Public, acessor.DeclaredAccessibility);
                        Assert.Null(test1.FindImplementationForInterfaceMember(acessor));
                    }
                }
            }

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate);
            Validate(compilation1.SourceModule);
        }

        [Fact]
        public void PropertyModifiers_14()
        {
            var source1 =
@"
public interface I1
{
    public sealed int P1 {get;} = 0; 
}
public interface I2
{
    abstract sealed int P2 {get;} 
}
public interface I3
{
    virtual sealed int P3 
    {
        set {}
    }
}

class Test1 : I1, I2, I3
{
    int I1.P1 { get => throw null; }
    int I2.P2 { get => throw null; }
    int I3.P3 { set => throw null; }
}

class Test2 : I1, I2, I3
{}
";
            ValidatePropertyModifiers_14(source1,
                // (4,23): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     public sealed int P1 {get;} = 0; 
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P1").WithArguments("I1.P1").WithLocation(4, 23),
                // (4,27): error CS0501: 'I1.P1.get' must declare a body because it is not marked abstract, extern, or partial
                //     public sealed int P1 {get;} = 0; 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P1.get").WithLocation(4, 27),
                // (8,25): error CS0238: 'I2.P2' cannot be sealed because it is not an override
                //     abstract sealed int P2 {get;} 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P2").WithArguments("I2.P2").WithLocation(8, 25),
                // (12,24): error CS0238: 'I3.P3' cannot be sealed because it is not an override
                //     virtual sealed int P3 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P3").WithArguments("I3.P3").WithLocation(12, 24),
                // (20,12): error CS0539: 'Test1.P1' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I1.P1 { get => throw null; }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P1").WithArguments("Test1.P1").WithLocation(20, 12),
                // (25,19): error CS0535: 'Test2' does not implement interface member 'I2.P2'
                // class Test2 : I1, I2, I3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Test2", "I2.P2").WithLocation(25, 19)
                );
        }

        private void ValidatePropertyModifiers_14(string source1, params DiagnosticDescription[] expected)
        { 
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(expected);

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");
            var p1 = GetSingleProperty(compilation1, "I1");
            var p1get = p1.GetMethod;

            Assert.False(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));
            Assert.Null(test2.FindImplementationForInterfaceMember(p1));

            Assert.False(p1get.IsAbstract);
            Assert.False(p1get.IsVirtual);
            Assert.False(p1get.IsMetadataVirtual());
            Assert.False(p1get.IsSealed);
            Assert.False(p1get.IsStatic);
            Assert.False(p1get.IsExtern);
            Assert.False(p1get.IsAsync);
            Assert.False(p1get.IsOverride);
            Assert.Equal(Accessibility.Public, p1get.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1get));
            Assert.Null(test2.FindImplementationForInterfaceMember(p1get));

            var p2 = GetSingleProperty(compilation1, "I2");
            var test1P2 = test1.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I2.")).Single();
            var p2get = p2.GetMethod;

            Assert.True(p2.IsAbstract);
            Assert.False(p2.IsVirtual);
            Assert.True(p2.IsSealed);
            Assert.False(p2.IsStatic);
            Assert.False(p2.IsExtern);
            Assert.False(p2.IsOverride);
            Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
            Assert.Same(test1P2, test1.FindImplementationForInterfaceMember(p2));
            Assert.Null(test2.FindImplementationForInterfaceMember(p2));

            Assert.True(p2get.IsAbstract);
            Assert.False(p2get.IsVirtual);
            Assert.True(p2get.IsMetadataVirtual());
            Assert.True(p2get.IsSealed);
            Assert.False(p2get.IsStatic);
            Assert.False(p2get.IsExtern);
            Assert.False(p2get.IsAsync);
            Assert.False(p2get.IsOverride);
            Assert.Equal(Accessibility.Public, p2get.DeclaredAccessibility);
            Assert.Same(test1P2.GetMethod, test1.FindImplementationForInterfaceMember(p2get));
            Assert.Null(test2.FindImplementationForInterfaceMember(p2get));

            var p3 = GetSingleProperty(compilation1, "I3");
            var test1P3 = test1.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I3.")).Single();
            var p3set = p3.SetMethod;

            Assert.False(p3.IsAbstract);
            Assert.True(p3.IsVirtual);
            Assert.True(p3.IsSealed);
            Assert.False(p3.IsStatic);
            Assert.False(p3.IsExtern);
            Assert.False(p3.IsOverride);
            Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
            Assert.Same(test1P3, test1.FindImplementationForInterfaceMember(p3));
            Assert.Same(p3, test2.FindImplementationForInterfaceMember(p3));

            Assert.False(p3set.IsAbstract);
            Assert.True(p3set.IsVirtual);
            Assert.True(p3set.IsMetadataVirtual());
            Assert.True(p3set.IsSealed);
            Assert.False(p3set.IsStatic);
            Assert.False(p3set.IsExtern);
            Assert.False(p3set.IsAsync);
            Assert.False(p3set.IsOverride);
            Assert.Equal(Accessibility.Public, p3set.DeclaredAccessibility);
            Assert.Same(test1P3.SetMethod, test1.FindImplementationForInterfaceMember(p3set));
            Assert.Same(p3set, test2.FindImplementationForInterfaceMember(p3set));
        }

        [Fact]
        public void PropertyModifiers_15()
        {
            var source1 =
@"
public interface I0
{
    abstract virtual int P0 { get; set; }
}
public interface I1
{
    abstract virtual int P1 { get { throw null; } }
}
public interface I2
{
    virtual abstract int P2 
    {
        get { throw null; }
        set { throw null; }
    }
}
public interface I3
{
    abstract virtual int P3 { set { throw null; } }
}
public interface I4
{
    abstract virtual int P4 { get => throw null; }
}
public interface I5
{
    abstract virtual int P5 
    {
        get => throw null;
        set => throw null;
    }
}
public interface I6
{
    abstract virtual int P6 { set => throw null; }
}
public interface I7
{
    abstract virtual int P7 => throw null;
}
public interface I8
{
    abstract virtual int P8 {get;} = 0;
}

class Test1 : I0, I1, I2, I3, I4, I5, I6, I7, I8
{
    int I0.P0 
    {
        get { throw null; }
        set { throw null; }
    }
    int I1.P1 
    {
        get { throw null; }
    }
    int I2.P2 
    {
        get { throw null; }
        set { throw null; }
    }
    int I3.P3 
    {
        set { throw null; }
    }
    int I4.P4 
    {
        get { throw null; }
    }
    int I5.P5 
    {
        get { throw null; }
        set { throw null; }
    }
    int I6.P6 
    {
        set { throw null; }
    }
    int I7.P7 
    {
        get { throw null; }
    }
    int I8.P8 
    {
        get { throw null; }
    }
}

class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
{}
";
            ValidatePropertyModifiers_15(source1,
                // (4,26): error CS0503: The abstract method 'I0.P0' cannot be marked virtual
                //     abstract virtual int P0 { get; set; }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P0").WithArguments("I0.P0"),
                // (8,26): error CS0503: The abstract method 'I1.P1' cannot be marked virtual
                //     abstract virtual int P1 { get { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P1").WithArguments("I1.P1").WithLocation(8, 26),
                // (8,31): error CS0500: 'I1.P1.get' cannot declare a body because it is marked abstract
                //     abstract virtual int P1 { get { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.P1.get").WithLocation(8, 31),
                // (12,26): error CS0503: The abstract method 'I2.P2' cannot be marked virtual
                //     virtual abstract int P2 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P2").WithArguments("I2.P2").WithLocation(12, 26),
                // (14,9): error CS0500: 'I2.P2.get' cannot declare a body because it is marked abstract
                //         get { throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I2.P2.get").WithLocation(14, 9),
                // (15,9): error CS0500: 'I2.P2.set' cannot declare a body because it is marked abstract
                //         set { throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I2.P2.set").WithLocation(15, 9),
                // (20,26): error CS0503: The abstract method 'I3.P3' cannot be marked virtual
                //     abstract virtual int P3 { set { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P3").WithArguments("I3.P3").WithLocation(20, 26),
                // (20,31): error CS0500: 'I3.P3.set' cannot declare a body because it is marked abstract
                //     abstract virtual int P3 { set { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I3.P3.set").WithLocation(20, 31),
                // (24,26): error CS0503: The abstract method 'I4.P4' cannot be marked virtual
                //     abstract virtual int P4 { get => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P4").WithArguments("I4.P4").WithLocation(24, 26),
                // (24,31): error CS0500: 'I4.P4.get' cannot declare a body because it is marked abstract
                //     abstract virtual int P4 { get => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I4.P4.get").WithLocation(24, 31),
                // (28,26): error CS0503: The abstract method 'I5.P5' cannot be marked virtual
                //     abstract virtual int P5 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P5").WithArguments("I5.P5").WithLocation(28, 26),
                // (30,9): error CS0500: 'I5.P5.get' cannot declare a body because it is marked abstract
                //         get => throw null;
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I5.P5.get").WithLocation(30, 9),
                // (31,9): error CS0500: 'I5.P5.set' cannot declare a body because it is marked abstract
                //         set => throw null;
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I5.P5.set").WithLocation(31, 9),
                // (36,26): error CS0503: The abstract method 'I6.P6' cannot be marked virtual
                //     abstract virtual int P6 { set => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P6").WithArguments("I6.P6").WithLocation(36, 26),
                // (36,31): error CS0500: 'I6.P6.set' cannot declare a body because it is marked abstract
                //     abstract virtual int P6 { set => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I6.P6.set").WithLocation(36, 31),
                // (40,26): error CS0503: The abstract method 'I7.P7' cannot be marked virtual
                //     abstract virtual int P7 => throw null;
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P7").WithArguments("I7.P7").WithLocation(40, 26),
                // (40,32): error CS0500: 'I7.P7.get' cannot declare a body because it is marked abstract
                //     abstract virtual int P7 => throw null;
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "throw null").WithArguments("I7.P7.get").WithLocation(40, 32),
                // (44,26): error CS0503: The abstract method 'I8.P8' cannot be marked virtual
                //     abstract virtual int P8 {get;} = 0;
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P8").WithArguments("I8.P8").WithLocation(44, 26),
                // (44,26): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     abstract virtual int P8 {get;} = 0;
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P8").WithArguments("I8.P8").WithLocation(44, 26),
                // (90,15): error CS0535: 'Test2' does not implement interface member 'I0.P0'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I0").WithArguments("Test2", "I0.P0").WithLocation(90, 15),
                // (90,19): error CS0535: 'Test2' does not implement interface member 'I1.P1'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.P1").WithLocation(90, 19),
                // (90,23): error CS0535: 'Test2' does not implement interface member 'I2.P2'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Test2", "I2.P2").WithLocation(90, 23),
                // (90,27): error CS0535: 'Test2' does not implement interface member 'I3.P3'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I3").WithArguments("Test2", "I3.P3").WithLocation(90, 27),
                // (90,31): error CS0535: 'Test2' does not implement interface member 'I4.P4'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I4").WithArguments("Test2", "I4.P4").WithLocation(90, 31),
                // (90,35): error CS0535: 'Test2' does not implement interface member 'I5.P5'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I5").WithArguments("Test2", "I5.P5").WithLocation(90, 35),
                // (90,39): error CS0535: 'Test2' does not implement interface member 'I6.P6'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I6").WithArguments("Test2", "I6.P6").WithLocation(90, 39),
                // (90,43): error CS0535: 'Test2' does not implement interface member 'I7.P7'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I7").WithArguments("Test2", "I7.P7").WithLocation(90, 43),
                // (90,47): error CS0535: 'Test2' does not implement interface member 'I8.P8'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I8").WithArguments("Test2", "I8.P8").WithLocation(90, 47)
                );
        }

        private void ValidatePropertyModifiers_15(string source1, params DiagnosticDescription[] expected)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(expected);

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");

            for (int i = 0; i <= 8; i++)
            {
                var i1 = compilation1.GetTypeByMetadataName("I" + i);
                var p2 = GetSingleProperty(i1);
                var test1P2 = test1.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith(i1.Name)).Single();

                Assert.True(p2.IsAbstract);
                Assert.True(p2.IsVirtual);
                Assert.False(p2.IsSealed);
                Assert.False(p2.IsStatic);
                Assert.False(p2.IsExtern);
                Assert.False(p2.IsOverride);
                Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
                Assert.Same(test1P2, test1.FindImplementationForInterfaceMember(p2));
                Assert.Null(test2.FindImplementationForInterfaceMember(p2));

                switch (i)
                {
                    case 3:
                    case 6:
                        Assert.Null(p2.GetMethod);
                        ValidateAccessor(p2.SetMethod, test1P2.SetMethod);
                        break;
                    case 1:
                    case 4:
                    case 7:
                    case 8:
                        Assert.Null(p2.SetMethod);
                        ValidateAccessor(p2.GetMethod, test1P2.GetMethod);
                        break;
                    default:
                        ValidateAccessor(p2.GetMethod, test1P2.GetMethod);
                        ValidateAccessor(p2.SetMethod, test1P2.SetMethod);
                        break;
                }

                void ValidateAccessor(MethodSymbol accessor, MethodSymbol implementedBy)
                {
                    Assert.True(accessor.IsAbstract);
                    Assert.True(accessor.IsVirtual);
                    Assert.True(accessor.IsMetadataVirtual());
                    Assert.False(accessor.IsSealed);
                    Assert.False(accessor.IsStatic);
                    Assert.False(accessor.IsExtern);
                    Assert.False(accessor.IsAsync);
                    Assert.False(accessor.IsOverride);
                    Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                    Assert.Same(implementedBy, test1.FindImplementationForInterfaceMember(accessor));
                    Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
                }
            }
        }

        [Fact]
        public void PropertyModifiers_16()
        {
            var source1 =
@"
public interface I1
{
    extern int P1 {get;} 
}
public interface I2
{
    virtual extern int P2 {set;}
}
public interface I3
{
    static extern int P3 {get; set;} 
}
public interface I4
{
    private extern int P4 {get;}
}
public interface I5
{
    extern sealed int P5 {set;}
}

class Test1 : I1, I2, I3, I4, I5
{
}

class Test2 : I1, I2, I3, I4, I5
{
    int I1.P1 => 0;
    int I2.P2 { set {} }
}
";
            ValidatePropertyModifiers_16(source1);
        }

        private void ValidatePropertyModifiers_16(string source1)
        { 
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var test2 = m.GlobalNamespace.GetTypeMember("Test2");
                bool isSource = !(m is PEModuleSymbol);
                var p1 = GetSingleProperty(m, "I1");
                var test2P1 = test2.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I1.")).Single();
                var p1get = p1.GetMethod;

                Assert.False(p1.IsAbstract);
                Assert.True(p1.IsVirtual);
                Assert.False(p1.IsSealed);
                Assert.False(p1.IsStatic);
                Assert.Equal(isSource, p1.IsExtern);
                Assert.False(p1.IsOverride);
                Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
                Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
                Assert.Same(test2P1, test2.FindImplementationForInterfaceMember(p1));

                Assert.False(p1get.IsAbstract);
                Assert.True(p1get.IsVirtual);
                Assert.True(p1get.IsMetadataVirtual());
                Assert.False(p1get.IsSealed);
                Assert.False(p1get.IsStatic);
                Assert.Equal(isSource, p1get.IsExtern);
                Assert.False(p1get.IsAsync);
                Assert.False(p1get.IsOverride);
                Assert.Equal(Accessibility.Public, p1get.DeclaredAccessibility);
                Assert.Same(p1get, test1.FindImplementationForInterfaceMember(p1get));
                Assert.Same(test2P1.GetMethod, test2.FindImplementationForInterfaceMember(p1get));

                var p2 = GetSingleProperty(m, "I2");
                var test2P2 = test2.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I2.")).Single();
                var p2set = p2.SetMethod;

                Assert.False(p2.IsAbstract);
                Assert.True(p2.IsVirtual);
                Assert.False(p2.IsSealed);
                Assert.False(p2.IsStatic);
                Assert.Equal(isSource, p2.IsExtern);
                Assert.False(p2.IsOverride);
                Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
                Assert.Same(p2, test1.FindImplementationForInterfaceMember(p2));
                Assert.Same(test2P2, test2.FindImplementationForInterfaceMember(p2));

                Assert.False(p2set.IsAbstract);
                Assert.True(p2set.IsVirtual);
                Assert.True(p2set.IsMetadataVirtual());
                Assert.False(p2set.IsSealed);
                Assert.False(p2set.IsStatic);
                Assert.Equal(isSource, p2set.IsExtern);
                Assert.False(p2set.IsAsync);
                Assert.False(p2set.IsOverride);
                Assert.Equal(Accessibility.Public, p2set.DeclaredAccessibility);
                Assert.Same(p2set, test1.FindImplementationForInterfaceMember(p2set));
                Assert.Same(test2P2.SetMethod, test2.FindImplementationForInterfaceMember(p2set));

                var i3 = m.ContainingAssembly.GetTypeByMetadataName("I3");

                if ((object)i3 != null)
                {
                    var p3 = GetSingleProperty(i3);

                    Assert.False(p3.IsAbstract);
                    Assert.False(p3.IsVirtual);
                    Assert.False(p3.IsSealed);
                    Assert.True(p3.IsStatic);
                    Assert.Equal(isSource, p3.IsExtern);
                    Assert.False(p3.IsOverride);
                    Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(p3));
                    Assert.Null(test2.FindImplementationForInterfaceMember(p3));

                    ValidateP3Accessor(p3.GetMethod);
                    ValidateP3Accessor(p3.SetMethod);
                    void ValidateP3Accessor(MethodSymbol accessor)
                    {
                        Assert.False(accessor.IsAbstract);
                        Assert.False(accessor.IsVirtual);
                        Assert.False(accessor.IsMetadataVirtual());
                        Assert.False(accessor.IsSealed);
                        Assert.True(accessor.IsStatic);
                        Assert.Equal(isSource, accessor.IsExtern);
                        Assert.False(accessor.IsAsync);
                        Assert.False(accessor.IsOverride);
                        Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                        Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                        Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
                    }
                }

                var p4 = GetSingleProperty(m, "I4");
                var p4get = p4.GetMethod;

                Assert.False(p4.IsAbstract);
                Assert.False(p4.IsVirtual);
                Assert.False(p4.IsSealed);
                Assert.False(p4.IsStatic);
                Assert.Equal(isSource, p4.IsExtern);
                Assert.False(p4.IsOverride);
                Assert.Equal(Accessibility.Private, p4.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(p4));
                Assert.Null(test2.FindImplementationForInterfaceMember(p4));

                Assert.False(p4get.IsAbstract);
                Assert.False(p4get.IsVirtual);
                Assert.False(p4get.IsMetadataVirtual());
                Assert.False(p4get.IsSealed);
                Assert.False(p4get.IsStatic);
                Assert.Equal(isSource, p4get.IsExtern);
                Assert.False(p4get.IsAsync);
                Assert.False(p4get.IsOverride);
                Assert.Equal(Accessibility.Private, p4get.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(p4get));
                Assert.Null(test2.FindImplementationForInterfaceMember(p4get));

                var p5 = GetSingleProperty(m, "I5");
                var p5set = p5.SetMethod;

                Assert.False(p5.IsAbstract);
                Assert.False(p5.IsVirtual);
                Assert.False(p5.IsSealed);
                Assert.False(p5.IsStatic);
                Assert.Equal(isSource, p5.IsExtern);
                Assert.False(p5.IsOverride);
                Assert.Equal(Accessibility.Public, p5.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(p5));
                Assert.Null(test2.FindImplementationForInterfaceMember(p5));

                Assert.False(p5set.IsAbstract);
                Assert.False(p5set.IsVirtual);
                Assert.False(p5set.IsMetadataVirtual());
                Assert.False(p5set.IsSealed);
                Assert.False(p5set.IsStatic);
                Assert.Equal(isSource, p5set.IsExtern);
                Assert.False(p5set.IsAsync);
                Assert.False(p5set.IsOverride);
                Assert.Equal(Accessibility.Public, p5set.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(p5set));
                Assert.Null(test2.FindImplementationForInterfaceMember(p5set));
            }
        }

        [Fact]
        public void PropertyModifiers_17()
        {
            var source1 =
@"
public interface I1
{
    abstract extern int P1 {get;} 
}
public interface I2
{
    extern int P2 => 0; 
}
public interface I3
{
    static extern int P3 {get => 0; set => throw null;} 
}
public interface I4
{
    private extern int P4 { get {throw null;} set {throw null;}}
}
public interface I5
{
    extern sealed int P5 {get;} = 0;
}

class Test1 : I1, I2, I3, I4, I5
{
}

class Test2 : I1, I2, I3, I4, I5
{
    int I1.P1 => 0;
    int I2.P2 => 0;
    int I3.P3 { get => 0; set => throw null;}
    int I4.P4 { get => 0; set => throw null;}
    int I5.P5 => 0;
}
";
            ValidatePropertyModifiers_17(source1,
                // (4,25): error CS0180: 'I1.P1' cannot be both extern and abstract
                //     abstract extern int P1 {get;} 
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "P1").WithArguments("I1.P1").WithLocation(4, 25),
                // (8,22): error CS0179: 'I2.P2.get' cannot be extern and declare a body
                //     extern int P2 => 0; 
                Diagnostic(ErrorCode.ERR_ExternHasBody, "0").WithArguments("I2.P2.get").WithLocation(8, 22),
                // (12,27): error CS0179: 'I3.P3.get' cannot be extern and declare a body
                //     static extern int P3 {get => 0; set => throw null;} 
                Diagnostic(ErrorCode.ERR_ExternHasBody, "get").WithArguments("I3.P3.get").WithLocation(12, 27),
                // (12,37): error CS0179: 'I3.P3.set' cannot be extern and declare a body
                //     static extern int P3 {get => 0; set => throw null;} 
                Diagnostic(ErrorCode.ERR_ExternHasBody, "set").WithArguments("I3.P3.set").WithLocation(12, 37),
                // (16,29): error CS0179: 'I4.P4.get' cannot be extern and declare a body
                //     private extern int P4 { get {throw null;} set {throw null;}}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "get").WithArguments("I4.P4.get").WithLocation(16, 29),
                // (16,47): error CS0179: 'I4.P4.set' cannot be extern and declare a body
                //     private extern int P4 { get {throw null;} set {throw null;}}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "set").WithArguments("I4.P4.set").WithLocation(16, 47),
                // (20,23): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     extern sealed int P5 {get;} = 0;
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P5").WithArguments("I5.P5").WithLocation(20, 23),
                // (23,15): error CS0535: 'Test1' does not implement interface member 'I1.P1'
                // class Test1 : I1, I2, I3, I4, I5
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.P1").WithLocation(23, 15),
                // (31,12): error CS0539: 'Test2.P3' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I3.P3 { get => 0; set => throw null;}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P3").WithArguments("Test2.P3").WithLocation(31, 12),
                // (32,12): error CS0539: 'Test2.P4' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I4.P4 { get => 0; set => throw null;}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P4").WithArguments("Test2.P4").WithLocation(32, 12),
                // (33,12): error CS0539: 'Test2.P5' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I5.P5 => 0;
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P5").WithArguments("Test2.P5").WithLocation(33, 12),
                // (20,27): warning CS0626: Method, operator, or accessor 'I5.P5.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern sealed int P5 {get;} = 0;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("I5.P5.get").WithLocation(20, 27)
                );
        }

        private void ValidatePropertyModifiers_17(string source1, params DiagnosticDescription[] expected)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(expected);

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");
            var p1 = GetSingleProperty(compilation1, "I1");
            var test2P1 = test2.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I1.")).Single();
            var p1get = p1.GetMethod;

            Assert.True(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.True(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(test2P1, test2.FindImplementationForInterfaceMember(p1));

            Assert.True(p1get.IsAbstract);
            Assert.False(p1get.IsVirtual);
            Assert.True(p1get.IsMetadataVirtual());
            Assert.False(p1get.IsSealed);
            Assert.False(p1get.IsStatic);
            Assert.True(p1get.IsExtern);
            Assert.False(p1get.IsAsync);
            Assert.False(p1get.IsOverride);
            Assert.Equal(Accessibility.Public, p1get.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1get));
            Assert.Same(test2P1.GetMethod, test2.FindImplementationForInterfaceMember(p1get));

            var p2 = GetSingleProperty(compilation1, "I2");
            var test2P2 = test2.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I2.")).Single();
            var p2get = p2.GetMethod;

            Assert.False(p2.IsAbstract);
            Assert.True(p2.IsVirtual);
            Assert.False(p2.IsSealed);
            Assert.False(p2.IsStatic);
            Assert.True(p2.IsExtern);
            Assert.False(p2.IsOverride);
            Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
            Assert.Same(p2, test1.FindImplementationForInterfaceMember(p2));
            Assert.Same(test2P2, test2.FindImplementationForInterfaceMember(p2));

            Assert.False(p2get.IsAbstract);
            Assert.True(p2get.IsVirtual);
            Assert.True(p2get.IsMetadataVirtual());
            Assert.False(p2get.IsSealed);
            Assert.False(p2get.IsStatic);
            Assert.True(p2get.IsExtern);
            Assert.False(p2get.IsAsync);
            Assert.False(p2get.IsOverride);
            Assert.Equal(Accessibility.Public, p2get.DeclaredAccessibility);
            Assert.Same(p2get, test1.FindImplementationForInterfaceMember(p2get));
            Assert.Same(test2P2.GetMethod, test2.FindImplementationForInterfaceMember(p2get));

            var p3 = GetSingleProperty(compilation1, "I3");
            var test2P3 = test2.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I3.")).Single();

            Assert.False(p3.IsAbstract);
            Assert.Equal(p3.IsIndexer, p3.IsVirtual);
            Assert.False(p3.IsSealed);
            Assert.Equal(!p3.IsIndexer, p3.IsStatic);
            Assert.True(p3.IsExtern);
            Assert.False(p3.IsOverride);
            Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
            Assert.Same(p3.IsIndexer ? p3 : null, test1.FindImplementationForInterfaceMember(p3));
            Assert.Same(p3.IsIndexer ? test2P3 : null, test2.FindImplementationForInterfaceMember(p3));

            ValidateP3Accessor(p3.GetMethod, p3.IsIndexer ? test2P3.GetMethod : null);
            ValidateP3Accessor(p3.SetMethod, p3.IsIndexer ? test2P3.SetMethod : null);
            void ValidateP3Accessor(MethodSymbol accessor, MethodSymbol test2Implementation)
            {
                Assert.False(accessor.IsAbstract);
                Assert.Equal(p3.IsIndexer, accessor.IsVirtual);
                Assert.Equal(p3.IsIndexer, accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.Equal(!p3.IsIndexer, accessor.IsStatic);
                Assert.True(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Same(p3.IsIndexer ? accessor : null, test1.FindImplementationForInterfaceMember(accessor));
                Assert.Same(test2Implementation, test2.FindImplementationForInterfaceMember(accessor));
            }

            var p4 = GetSingleProperty(compilation1, "I4");

            Assert.False(p4.IsAbstract);
            Assert.False(p4.IsVirtual);
            Assert.False(p4.IsSealed);
            Assert.False(p4.IsStatic);
            Assert.True(p4.IsExtern);
            Assert.False(p4.IsOverride);
            Assert.Equal(Accessibility.Private, p4.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p4));
            Assert.Null(test2.FindImplementationForInterfaceMember(p4));

            ValidateP4Accessor(p4.GetMethod);
            ValidateP4Accessor(p4.SetMethod);
            void ValidateP4Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.False(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.True(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Private, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
            }

            var p5 = GetSingleProperty(compilation1, "I5");
            var p5get = p5.GetMethod;

            Assert.False(p5.IsAbstract);
            Assert.False(p5.IsVirtual);
            Assert.False(p5.IsSealed);
            Assert.False(p5.IsStatic);
            Assert.True(p5.IsExtern);
            Assert.False(p5.IsOverride);
            Assert.Equal(Accessibility.Public, p5.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p5));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5));

            Assert.False(p5get.IsAbstract);
            Assert.False(p5get.IsVirtual);
            Assert.False(p5get.IsMetadataVirtual());
            Assert.False(p5get.IsSealed);
            Assert.False(p5get.IsStatic);
            Assert.True(p5get.IsExtern);
            Assert.False(p5get.IsAsync);
            Assert.False(p5get.IsOverride);
            Assert.Equal(Accessibility.Public, p5get.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p5get));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5get));
        }

        [Fact]
        public void PropertyModifiers_18()
        {
            var source1 =
@"
public interface I1
{
    abstract int P1 {get => 0; set => throw null;} 
}
public interface I2
{
    abstract private int P2 => 0; 
}
public interface I3
{
    static extern int P3 {get; set;} 
}
public interface I4
{
    abstract static int P4 { get {throw null;} set {throw null;}}
}
public interface I5
{
    override sealed int P5 {get;} = 0;
}

class Test1 : I1, I2, I3, I4, I5
{
}

class Test2 : I1, I2, I3, I4, I5
{
    int I1.P1 { get => 0; set => throw null;}
    int I2.P2 => 0;
    int I3.P3 { get => 0; set => throw null;}
    int I4.P4 { get => 0; set => throw null;}
    int I5.P5 => 0;
}
";
            ValidatePropertyModifiers_18(source1,
                // (4,22): error CS0500: 'I1.P1.get' cannot declare a body because it is marked abstract
                //     abstract int P1 {get => 0; set => throw null;} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.P1.get"),
                // (4,32): error CS0500: 'I1.P1.set' cannot declare a body because it is marked abstract
                //     abstract int P1 {get => 0; set => throw null;} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I1.P1.set"),
                // (8,26): error CS0621: 'I2.P2': virtual or abstract members cannot be private
                //     abstract private int P2 => 0; 
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "P2").WithArguments("I2.P2").WithLocation(8, 26),
                // (8,32): error CS0500: 'I2.P2.get' cannot declare a body because it is marked abstract
                //     abstract private int P2 => 0; 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "0").WithArguments("I2.P2.get").WithLocation(8, 32),
                // (16,25): error CS0112: A static member 'I4.P4' cannot be marked as override, virtual, or abstract
                //     abstract static int P4 { get {throw null;} set {throw null;}}
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "P4").WithArguments("I4.P4").WithLocation(16, 25),
                // (16,30): error CS0500: 'I4.P4.get' cannot declare a body because it is marked abstract
                //     abstract static int P4 { get {throw null;} set {throw null;}}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I4.P4.get").WithLocation(16, 30),
                // (16,48): error CS0500: 'I4.P4.set' cannot declare a body because it is marked abstract
                //     abstract static int P4 { get {throw null;} set {throw null;}}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I4.P4.set").WithLocation(16, 48),
                // (20,25): error CS0106: The modifier 'override' is not valid for this item
                //     override sealed int P5 {get;} = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P5").WithArguments("override").WithLocation(20, 25),
                // (20,25): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     override sealed int P5 {get;} = 0;
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P5").WithArguments("I5.P5").WithLocation(20, 25),
                // (20,29): error CS0501: 'I5.P5.get' must declare a body because it is not marked abstract, extern, or partial
                //     override sealed int P5 {get;} = 0;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I5.P5.get").WithLocation(20, 29),
                // (23,15): error CS0535: 'Test1' does not implement interface member 'I1.P1'
                // class Test1 : I1, I2, I3, I4, I5
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.P1").WithLocation(23, 15),
                // (23,19): error CS0535: 'Test1' does not implement interface member 'I2.P2'
                // class Test1 : I1, I2, I3, I4, I5
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Test1", "I2.P2").WithLocation(23, 19),
                // (31,12): error CS0539: 'Test2.P3' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I3.P3 { get => 0; set => throw null;}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P3").WithArguments("Test2.P3").WithLocation(31, 12),
                // (32,12): error CS0539: 'Test2.P4' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I4.P4 { get => 0; set => throw null;}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P4").WithArguments("Test2.P4").WithLocation(32, 12),
                // (33,12): error CS0539: 'Test2.P5' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I5.P5 => 0;
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P5").WithArguments("Test2.P5").WithLocation(33, 12),
                // (12,27): warning CS0626: Method, operator, or accessor 'I3.P3.get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     static extern int P3 {get; set;} 
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("I3.P3.get").WithLocation(12, 27),
                // (12,32): warning CS0626: Method, operator, or accessor 'I3.P3.set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     static extern int P3 {get; set;} 
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("I3.P3.set").WithLocation(12, 32)
                );
        }

        private void ValidatePropertyModifiers_18(string source1, params DiagnosticDescription[] expected)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(expected);

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");
            var p1 = GetSingleProperty(compilation1, "I1");
            var test2P1 = test2.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I1.")).Single();

            Assert.True(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(test2P1, test2.FindImplementationForInterfaceMember(p1));

            ValidateP1Accessor(p1.GetMethod, test2P1.GetMethod);
            ValidateP1Accessor(p1.SetMethod, test2P1.SetMethod);
            void ValidateP1Accessor(MethodSymbol accessor, MethodSymbol implementation)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Same(implementation, test2.FindImplementationForInterfaceMember(accessor));
            }

            var p2 = GetSingleProperty(compilation1, "I2");
            var test2P2 = test2.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I2.")).Single();
            var p2get = p2.GetMethod;

            Assert.True(p2.IsAbstract);
            Assert.False(p2.IsVirtual);
            Assert.False(p2.IsSealed);
            Assert.False(p2.IsStatic);
            Assert.False(p2.IsExtern);
            Assert.False(p2.IsOverride);
            Assert.Equal(Accessibility.Private, p2.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p2));
            Assert.Same(test2P2, test2.FindImplementationForInterfaceMember(p2));

            Assert.True(p2get.IsAbstract);
            Assert.False(p2get.IsVirtual);
            Assert.True(p2get.IsMetadataVirtual());
            Assert.False(p2get.IsSealed);
            Assert.False(p2get.IsStatic);
            Assert.False(p2get.IsExtern);
            Assert.False(p2get.IsAsync);
            Assert.False(p2get.IsOverride);
            Assert.Equal(Accessibility.Private, p2get.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p2get));
            Assert.Same(test2P2.GetMethod, test2.FindImplementationForInterfaceMember(p2get));

            var p3 = GetSingleProperty(compilation1, "I3");
            var test2P3 = test2.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I3.")).Single();

            Assert.False(p3.IsAbstract);
            Assert.Equal(p3.IsIndexer, p3.IsVirtual);
            Assert.False(p3.IsSealed);
            Assert.Equal(!p3.IsIndexer, p3.IsStatic);
            Assert.True(p3.IsExtern);
            Assert.False(p3.IsOverride);
            Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
            Assert.Same(p3.IsIndexer ? p3 : null, test1.FindImplementationForInterfaceMember(p3));
            Assert.Same(p3.IsIndexer ? test2P3 : null, test2.FindImplementationForInterfaceMember(p3));

            ValidateP3Accessor(p3.GetMethod, p3.IsIndexer ? test2P3.GetMethod : null);
            ValidateP3Accessor(p3.SetMethod, p3.IsIndexer ? test2P3.SetMethod : null);
            void ValidateP3Accessor(MethodSymbol accessor, MethodSymbol implementation)
            {
                Assert.False(accessor.IsAbstract);
                Assert.Equal(p3.IsIndexer, accessor.IsVirtual);
                Assert.Equal(p3.IsIndexer, accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.Equal(!p3.IsIndexer, accessor.IsStatic);
                Assert.True(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Same(p3.IsIndexer ? accessor : null, test1.FindImplementationForInterfaceMember(accessor));
                Assert.Same(implementation, test2.FindImplementationForInterfaceMember(accessor));
            }

            var p4 = GetSingleProperty(compilation1, "I4");
            var test2P4 = test2.GetMembers().OfType<PropertySymbol>().Where(p => p.Name.StartsWith("I4.")).Single();

            Assert.True(p4.IsAbstract);
            Assert.False(p4.IsVirtual);
            Assert.False(p4.IsSealed);
            Assert.Equal(!p4.IsIndexer, p4.IsStatic);
            Assert.False(p4.IsExtern);
            Assert.False(p4.IsOverride);
            Assert.Equal(Accessibility.Public, p4.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p4));
            Assert.Same(p4.IsIndexer ? test2P4 : null, test2.FindImplementationForInterfaceMember(p4));

            ValidateP4Accessor(p4.GetMethod, p4.IsIndexer ? test2P4.GetMethod : null);
            ValidateP4Accessor(p4.SetMethod, p4.IsIndexer ? test2P4.SetMethod : null);
            void ValidateP4Accessor(MethodSymbol accessor, MethodSymbol implementation)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.Equal(!p4.IsIndexer, accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Same(implementation, test2.FindImplementationForInterfaceMember(accessor));
            }

            var p5 = GetSingleProperty(compilation1, "I5");
            var p5get = p5.GetMethod;

            Assert.False(p5.IsAbstract);
            Assert.False(p5.IsVirtual);
            Assert.False(p5.IsSealed);
            Assert.False(p5.IsStatic);
            Assert.False(p5.IsExtern);
            Assert.False(p5.IsOverride);
            Assert.Equal(Accessibility.Public, p5.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p5));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5));

            Assert.False(p5get.IsAbstract);
            Assert.False(p5get.IsVirtual);
            Assert.False(p5get.IsMetadataVirtual());
            Assert.False(p5get.IsSealed);
            Assert.False(p5get.IsStatic);
            Assert.False(p5get.IsExtern);
            Assert.False(p5get.IsAsync);
            Assert.False(p5get.IsOverride);
            Assert.Equal(Accessibility.Public, p5get.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p5get));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5get));
        }

        [Fact]
        public void PropertyModifiers_19()
        {
            var source1 =
@"

public interface I2 {}

public interface I1
{
    public int I2.P01 {get; set;}
    protected int I2.P02 {get;}
    protected internal int I2.P03 {set;}
    internal int I2.P04 {get;}
    private int I2.P05 {set;}
    static int I2.P06 {get;}
    virtual int I2.P07 {set;}
    sealed int I2.P08 {get;}
    override int I2.P09 {set;}
    abstract int I2.P10 {get;}
    extern int I2.P11 {get; set;}

    int I2.P12 { public get; set;}
    int I2.P13 { get; protected set;}
    int I2.P14 { protected internal get; set;}
    int I2.P15 { get; internal set;}
    int I2.P16 { private get; set;}
    int I2.P17 { private get;}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            var expected = new[]
            {
                // (7,19): error CS0106: The modifier 'public' is not valid for this item
                //     public int I2.P01 {get; set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P01").WithArguments("public").WithLocation(7, 19),
                // (7,19): error CS0541: 'I1.P01': explicit interface declaration can only be declared in a class or struct
                //     public int I2.P01 {get; set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P01").WithArguments("I1.P01").WithLocation(7, 19),
                // (8,22): error CS0106: The modifier 'protected' is not valid for this item
                //     protected int I2.P02 {get;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P02").WithArguments("protected").WithLocation(8, 22),
                // (8,22): error CS0541: 'I1.P02': explicit interface declaration can only be declared in a class or struct
                //     protected int I2.P02 {get;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P02").WithArguments("I1.P02").WithLocation(8, 22),
                // (9,31): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal int I2.P03 {set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P03").WithArguments("protected internal").WithLocation(9, 31),
                // (9,31): error CS0541: 'I1.P03': explicit interface declaration can only be declared in a class or struct
                //     protected internal int I2.P03 {set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P03").WithArguments("I1.P03").WithLocation(9, 31),
                // (10,21): error CS0106: The modifier 'internal' is not valid for this item
                //     internal int I2.P04 {get;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P04").WithArguments("internal").WithLocation(10, 21),
                // (10,21): error CS0541: 'I1.P04': explicit interface declaration can only be declared in a class or struct
                //     internal int I2.P04 {get;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P04").WithArguments("I1.P04").WithLocation(10, 21),
                // (11,20): error CS0106: The modifier 'private' is not valid for this item
                //     private int I2.P05 {set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P05").WithArguments("private").WithLocation(11, 20),
                // (11,20): error CS0541: 'I1.P05': explicit interface declaration can only be declared in a class or struct
                //     private int I2.P05 {set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P05").WithArguments("I1.P05").WithLocation(11, 20),
                // (12,19): error CS0106: The modifier 'static' is not valid for this item
                //     static int I2.P06 {get;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P06").WithArguments("static").WithLocation(12, 19),
                // (12,19): error CS0541: 'I1.P06': explicit interface declaration can only be declared in a class or struct
                //     static int I2.P06 {get;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P06").WithArguments("I1.P06").WithLocation(12, 19),
                // (13,20): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual int I2.P07 {set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P07").WithArguments("virtual").WithLocation(13, 20),
                // (13,20): error CS0541: 'I1.P07': explicit interface declaration can only be declared in a class or struct
                //     virtual int I2.P07 {set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P07").WithArguments("I1.P07").WithLocation(13, 20),
                // (14,19): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed int I2.P08 {get;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P08").WithArguments("sealed").WithLocation(14, 19),
                // (14,19): error CS0541: 'I1.P08': explicit interface declaration can only be declared in a class or struct
                //     sealed int I2.P08 {get;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P08").WithArguments("I1.P08").WithLocation(14, 19),
                // (15,21): error CS0106: The modifier 'override' is not valid for this item
                //     override int I2.P09 {set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P09").WithArguments("override").WithLocation(15, 21),
                // (15,21): error CS0541: 'I1.P09': explicit interface declaration can only be declared in a class or struct
                //     override int I2.P09 {set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P09").WithArguments("I1.P09").WithLocation(15, 21),
                // (16,21): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract int I2.P10 {get;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P10").WithArguments("abstract").WithLocation(16, 21),
                // (16,21): error CS0541: 'I1.P10': explicit interface declaration can only be declared in a class or struct
                //     abstract int I2.P10 {get;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P10").WithArguments("I1.P10").WithLocation(16, 21),
                // (17,19): error CS0106: The modifier 'extern' is not valid for this item
                //     extern int I2.P11 {get; set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P11").WithArguments("extern").WithLocation(17, 19),
                // (17,19): error CS0541: 'I1.P11': explicit interface declaration can only be declared in a class or struct
                //     extern int I2.P11 {get; set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P11").WithArguments("I1.P11").WithLocation(17, 19),
                // (19,12): error CS0541: 'I1.P12': explicit interface declaration can only be declared in a class or struct
                //     int I2.P12 { public get; set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P12").WithArguments("I1.P12").WithLocation(19, 12),
                // (19,25): error CS0106: The modifier 'public' is not valid for this item
                //     int I2.P12 { public get; set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("public").WithLocation(19, 25),
                // (20,12): error CS0541: 'I1.P13': explicit interface declaration can only be declared in a class or struct
                //     int I2.P13 { get; protected set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P13").WithArguments("I1.P13").WithLocation(20, 12),
                // (20,33): error CS0106: The modifier 'protected' is not valid for this item
                //     int I2.P13 { get; protected set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("protected").WithLocation(20, 33),
                // (21,12): error CS0541: 'I1.P14': explicit interface declaration can only be declared in a class or struct
                //     int I2.P14 { protected internal get; set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P14").WithArguments("I1.P14").WithLocation(21, 12),
                // (21,37): error CS0106: The modifier 'protected internal' is not valid for this item
                //     int I2.P14 { protected internal get; set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("protected internal").WithLocation(21, 37),
                // (22,12): error CS0541: 'I1.P15': explicit interface declaration can only be declared in a class or struct
                //     int I2.P15 { get; internal set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P15").WithArguments("I1.P15").WithLocation(22, 12),
                // (22,32): error CS0106: The modifier 'internal' is not valid for this item
                //     int I2.P15 { get; internal set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("internal").WithLocation(22, 32),
                // (23,12): error CS0541: 'I1.P16': explicit interface declaration can only be declared in a class or struct
                //     int I2.P16 { private get; set;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P16").WithArguments("I1.P16").WithLocation(23, 12),
                // (23,26): error CS0106: The modifier 'private' is not valid for this item
                //     int I2.P16 { private get; set;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("private").WithLocation(23, 26),
                // (24,12): error CS0541: 'I1.P17': explicit interface declaration can only be declared in a class or struct
                //     int I2.P17 { private get;}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P17").WithArguments("I1.P17").WithLocation(24, 12),
                // (24,26): error CS0106: The modifier 'private' is not valid for this item
                //     int I2.P17 { private get;}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("private").WithLocation(24, 26)
            };

            compilation1.VerifyDiagnostics(expected);

            ValidateSymbolsPropertyModifiers_19(compilation1);

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(expected);

            ValidateSymbolsPropertyModifiers_19(compilation2);
        }

        private static void ValidateSymbolsPropertyModifiers_19(CSharpCompilation compilation1)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");

            foreach (var propertyName in new[] { "I2.P01", "I2.P02", "I2.P03", "I2.P04", "I2.P05", "I2.P06", "I2.P07", "I2.P08", "I2.P09", "I2.P10",
                                                 "I2.P11", "I2.P12", "I2.P13", "I2.P14", "I2.P15", "I2.P16", "I2.P17" })
            {
                ValidateSymbolsPropertyModifiers_19(i1.GetMember<PropertySymbol>(propertyName));

            }
        }

        private static void ValidateSymbolsPropertyModifiers_19(PropertySymbol p01)
        {
            Assert.True(p01.IsAbstract);
            Assert.False(p01.IsVirtual);
            Assert.False(p01.IsSealed);
            Assert.False(p01.IsStatic);
            Assert.False(p01.IsExtern);
            Assert.False(p01.IsOverride);
            Assert.Equal(Accessibility.Public, p01.DeclaredAccessibility);

            ValidateAccessor(p01.GetMethod);
            ValidateAccessor(p01.SetMethod);

            void ValidateAccessor(MethodSymbol accessor)
            {
                if ((object)accessor == null)
                {
                    return;
                }

                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }
        }

        [Fact]
        public void PropertyModifiers_20()
        {
            var source1 =
@"
public interface I1
{
    internal int P1
    {
        get 
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set 
        {
            System.Console.WriteLine(""set_P1"");
        }
    }

    void M2() {P1 = P1;}
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.M2();
    }
}
";

            ValidatePropertyModifiers_20(source1, source2);
        }

        private void ValidatePropertyModifiers_20(string source1, string source2)
        {
            var compilation1 = CreateStandardCompilation(source1 + source2, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            Validate1(compilation1.SourceModule);

            void Validate1(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = test1.Interfaces.Single();
                var p1 = GetSingleProperty(i1);
                var p1get = p1.GetMethod;
                var p1set = p1.SetMethod;

                ValidateProperty(p1);
                ValidateMethod(p1get);
                ValidateMethod(p1set);
                Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
                Assert.Same(p1get, test1.FindImplementationForInterfaceMember(p1get));
                Assert.Same(p1set, test1.FindImplementationForInterfaceMember(p1set));
            }

            void ValidateProperty(PropertySymbol p1)
            {
                Assert.False(p1.IsAbstract);
                Assert.True(p1.IsVirtual);
                Assert.False(p1.IsSealed);
                Assert.False(p1.IsStatic);
                Assert.False(p1.IsExtern);
                Assert.False(p1.IsOverride);
                Assert.Equal(Accessibility.Internal, p1.DeclaredAccessibility);
            }

            void ValidateMethod(MethodSymbol m1)
            {
                Assert.False(m1.IsAbstract);
                Assert.True(m1.IsVirtual);
                Assert.True(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(Accessibility.Internal, m1.DeclaredAccessibility);
            }

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            {
                var i1 = compilation2.GetTypeByMetadataName("I1");
                var p1 = GetSingleProperty(i1);
                var p1get = p1.GetMethod;
                var p1set = p1.SetMethod;

                ValidateProperty(p1);
                ValidateMethod(p1get);
                ValidateMethod(p1set);
            }

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation2.ToMetadataReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate1);

            Validate1(compilation3.SourceModule);

            var compilation4 = CreateStandardCompilation(source2, new[] { compilation2.EmitToImageReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation4.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation4, verify: false, symbolValidator: Validate1);

            Validate1(compilation4.SourceModule);
        }

        [Fact]
        public void PropertyModifiers_21()
        {
            var source1 =
@"
public interface I1
{
    private static int P1 { get => throw null; set => throw null; }

    internal static int P2 { get => throw null; set => throw null; }

    public static int P3 { get => throw null; set => throw null; }

    static int P4 { get => throw null; set => throw null; }
}

class Test1
{
    static void Main()
    {
        int x;
        x = I1.P1;
        I1.P1 = x;
        x = I1.P2;
        I1.P2 = x;
        x = I1.P3;
        I1.P3 = x;
        x = I1.P4;
        I1.P4 = x;
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (18,16): error CS0122: 'I1.P1' is inaccessible due to its protection level
                //         x = I1.P1;
                Diagnostic(ErrorCode.ERR_BadAccess, "P1").WithArguments("I1.P1").WithLocation(18, 16),
                // (19,12): error CS0122: 'I1.P1' is inaccessible due to its protection level
                //         I1.P1 = x;
                Diagnostic(ErrorCode.ERR_BadAccess, "P1").WithArguments("I1.P1").WithLocation(19, 12)
                );

            var source2 =
@"
class Test2
{
    static void Main()
    {
        int x;
        x = I1.P1;
        I1.P1 = x;
        x = I1.P2;
        I1.P2 = x;
        x = I1.P3;
        I1.P3 = x;
        x = I1.P4;
        I1.P4 = x;
    }
}
";
            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() },
                                                         options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(
                // (7,16): error CS0122: 'I1.P1' is inaccessible due to its protection level
                //         x = I1.P1;
                Diagnostic(ErrorCode.ERR_BadAccess, "P1").WithArguments("I1.P1").WithLocation(7, 16),
                // (8,12): error CS0122: 'I1.P1' is inaccessible due to its protection level
                //         I1.P1 = x;
                Diagnostic(ErrorCode.ERR_BadAccess, "P1").WithArguments("I1.P1").WithLocation(8, 12),
                // (9,16): error CS0122: 'I1.P2' is inaccessible due to its protection level
                //         x = I1.P2;
                Diagnostic(ErrorCode.ERR_BadAccess, "P2").WithArguments("I1.P2").WithLocation(9, 16),
                // (10,12): error CS0122: 'I1.P2' is inaccessible due to its protection level
                //         I1.P2 = x;
                Diagnostic(ErrorCode.ERR_BadAccess, "P2").WithArguments("I1.P2").WithLocation(10, 12)
                );
        }

        [Fact]
        public void PropertyModifiers_22()
        {
            var source1 =
@"
public interface I1
{
    public int P1 
    {
        internal get
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set 
        {
            System.Console.WriteLine(""set_P1"");
        }
    }
}
public interface I2
{
    int P2 
    {
        get
        {
            System.Console.WriteLine(""get_P2"");
            return 0;
        }
        internal set
        {
            System.Console.WriteLine(""set_P2"");
        }
    }
}
public interface I3
{
    int P3 
    {
        internal get => Test1.GetP3();
        set => System.Console.WriteLine(""set_P3"");
    }
}
public interface I4
{
    int P4
    {
        get => Test1.GetP4();
        internal set => System.Console.WriteLine(""set_P4"");
    }
}
public interface I5
{
    int P5 
    {
        private get
        {
            System.Console.WriteLine(""get_P5"");
            return 0;
        }
        set 
        {
            System.Console.WriteLine(""set_P5"");
        }
    }

    void Test()
    {
        P5 = P5;
    }
}
public interface I6
{
    int P6 
    {
        get
        {
            System.Console.WriteLine(""get_P6"");
            return 0;
        }
        private set
        {
            System.Console.WriteLine(""set_P6"");
        }
    }

    void Test()
    {
        P6 = P6;
    }
}
public interface I7
{
    int P7 
    {
        private get => Test1.GetP7();
        set => System.Console.WriteLine(""set_P7"");
    }

    void Test()
    {
        P7 = P7;
    }
}
public interface I8
{
    int P8
    {
        get => Test1.GetP8();
        private set => System.Console.WriteLine(""set_P8"");
    }

    void Test()
    {
        P8 = P8;
    }
}

class Test1 : I1, I2, I3, I4, I5, I6, I7, I8
{
    static void Main()
    {
        I1 i1 = new Test1();
        I2 i2 = new Test1();
        I3 i3 = new Test1();
        I4 i4 = new Test1();
        I5 i5 = new Test1();
        I6 i6 = new Test1();
        I7 i7 = new Test1();
        I8 i8 = new Test1();

        i1.P1 = i1.P1;
        i2.P2 = i2.P2;
        i3.P3 = i3.P3;
        i4.P4 = i4.P4;
        i5.Test();
        i6.Test();
        i7.Test();
        i8.Test();
    }

    public static int GetP3()
    {
        System.Console.WriteLine(""get_P3"");
        return 0;
    }

    public static int GetP4()
    {
        System.Console.WriteLine(""get_P4"");
        return 0;
    }

    public static int GetP7()
    {
        System.Console.WriteLine(""get_P7"");
        return 0;
    }

    public static int GetP8()
    {
        System.Console.WriteLine(""get_P8"");
        return 0;
    }
}
";

            ValidatePropertyModifiers_22(source1);
        }

        private void ValidatePropertyModifiers_22(string source1)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, /*expectedOutput:
@"get_P1
set_P1
get_P2
set_P2
get_P3
set_P3
get_P4
set_P4
get_P5
set_P5
get_P6
set_P6
get_P7
set_P7
get_P8
set_P8",*/ symbolValidator: Validate, verify: false);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");

                for (int i = 1; i <= 8; i++)
                {
                    var i1 = m.GlobalNamespace.GetTypeMember("I" + i);
                    var p1 = GetSingleProperty(i1);

                    Assert.False(p1.IsAbstract);
                    Assert.True(p1.IsVirtual);
                    Assert.False(p1.IsSealed);
                    Assert.False(p1.IsStatic);
                    Assert.False(p1.IsExtern);
                    Assert.False(p1.IsOverride);
                    Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
                    Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));

                    switch (i)
                    {
                        case 1:
                        case 3:
                            ValidateAccessor(p1.GetMethod, Accessibility.Internal);
                            ValidateAccessor(p1.SetMethod, Accessibility.Public);
                            break;
                        case 2:
                        case 4:
                            ValidateAccessor(p1.GetMethod, Accessibility.Public);
                            ValidateAccessor(p1.SetMethod, Accessibility.Internal);
                            break;
                        case 5:
                        case 7:
                            ValidateAccessor(p1.GetMethod, Accessibility.Private);
                            ValidateAccessor(p1.SetMethod, Accessibility.Public);
                            break;
                        case 6:
                        case 8:
                            ValidateAccessor(p1.GetMethod, Accessibility.Public);
                            ValidateAccessor(p1.SetMethod, Accessibility.Private);
                            break;
                        default:
                            Assert.False(true);
                            break;
                    }

                    void ValidateAccessor(MethodSymbol accessor, Accessibility access)
                    {
                        Assert.False(accessor.IsAbstract);
                        Assert.Equal(accessor.DeclaredAccessibility != Accessibility.Private, accessor.IsVirtual);
                        Assert.Equal(accessor.DeclaredAccessibility != Accessibility.Private, accessor.IsMetadataVirtual());
                        Assert.False(accessor.IsSealed);
                        Assert.False(accessor.IsStatic);
                        Assert.False(accessor.IsExtern);
                        Assert.False(accessor.IsAsync);
                        Assert.False(accessor.IsOverride);
                        Assert.Equal(access, accessor.DeclaredAccessibility);
                        Assert.Same(accessor.DeclaredAccessibility == Accessibility.Private ? null : accessor, test1.FindImplementationForInterfaceMember(accessor));
                    }
                }
            }
        }

        [Fact]
        public void PropertyModifiers_23()
        {
            var source1 =
@"
public interface I1
{
    abstract int P1 {internal get; set;} 

    void M2()
    {
        P1 = P1;
    }
}
public interface I3
{
    int P3
    {
        private get 
        {
            System.Console.WriteLine(""get_P3"");
            return 0;
        } 
        set {}
    }

    void M2()
    {
        P3 = P3;
    }
}
public interface I4
{
    int P4
    {
        get {throw null;} 
        private set {System.Console.WriteLine(""set_P4"");}
    }

    void M2()
    {
        P4 = P4;
    }
}
public interface I5
{
    int P5
    {
        private get => GetP5();
        set => throw null;
    }

    private int GetP5()
    {
        System.Console.WriteLine(""get_P5"");
        return 0;
    }

    void M2()
    {
        P5 = P5;
    }
}
public interface I6
{
    int P6
    {
        get => throw null;
        private set => System.Console.WriteLine(""set_P6"");
    }

    void M2()
    {
        P6 = P6;
    }
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 i1 = new Test1();
        I3 i3 = new Test3();
        I4 i4 = new Test4();
        I5 i5 = new Test5();
        I6 i6 = new Test6();
        i1.M2();
        i3.M2();
        i4.M2();
        i5.M2();
        i6.M2();
    }

    public int P1 
    {
        get
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""set_P1"");
        }
    }
}
class Test3 : I3
{
    public int P3 
    {
        get
        {
            throw null;
        }
        set
        {
            System.Console.WriteLine(""set_P3"");
        }
    }
}
class Test4 : I4
{
    public int P4 
    {
        get
        {
            System.Console.WriteLine(""get_P4"");
            return 0;
        }
        set
        {
            throw null;
        }
    }
}
class Test5 : I5
{
    public int P5 
    {
        get
        {
            throw null;
        }
        set
        {
            System.Console.WriteLine(""set_P5"");
        }
    }
}
class Test6 : I6
{
    public int P6 
    {
        get
        {
            System.Console.WriteLine(""get_P6"");
            return 0;
        }
        set
        {
            throw null;
        }
    }
}
";
            ValidatePropertyModifiers_23(source1, source2);
        }

        private void ValidatePropertyModifiers_23(string source1, string source2)
        {
            var compilation1 = CreateStandardCompilation(source1 + source2, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            Validate1(compilation1.SourceModule);

            void Validate1(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var im = test1.Interfaces.Single().ContainingModule;

                ValidateProperty(GetSingleProperty(im, "I1"), true, Accessibility.Internal, Accessibility.Public, test1);
                ValidateProperty(GetSingleProperty(im, "I3"), false, Accessibility.Private, Accessibility.Public, m.GlobalNamespace.GetTypeMember("Test3"));
                ValidateProperty(GetSingleProperty(im, "I4"), false, Accessibility.Public, Accessibility.Private, m.GlobalNamespace.GetTypeMember("Test4"));
                ValidateProperty(GetSingleProperty(im, "I5"), false, Accessibility.Private, Accessibility.Public, m.GlobalNamespace.GetTypeMember("Test5"));
                ValidateProperty(GetSingleProperty(im, "I6"), false, Accessibility.Public, Accessibility.Private, m.GlobalNamespace.GetTypeMember("Test6"));
            }

            void ValidateProperty(PropertySymbol p1, bool isAbstract, Accessibility getAccess, Accessibility setAccess, NamedTypeSymbol test1 = null)
            {
                Assert.Equal(isAbstract, p1.IsAbstract);
                Assert.NotEqual(isAbstract, p1.IsVirtual);
                Assert.False(p1.IsSealed);
                Assert.False(p1.IsStatic);
                Assert.False(p1.IsExtern);
                Assert.False(p1.IsOverride);
                Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);

                if ((object)test1 != null)
                {
                    Assert.Same(test1.GetMember(p1.Name), test1.FindImplementationForInterfaceMember(p1));
                }

                ValidateMethod(p1.GetMethod, isAbstract, getAccess, test1);
                ValidateMethod(p1.SetMethod, isAbstract, setAccess, test1);
            }

            void ValidateMethod(MethodSymbol m1, bool isAbstract, Accessibility access, NamedTypeSymbol test1)
            {
                Assert.Equal(isAbstract, m1.IsAbstract);
                Assert.NotEqual(isAbstract || access == Accessibility.Private, m1.IsVirtual);
                Assert.Equal(isAbstract || access != Accessibility.Private, m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(access, m1.DeclaredAccessibility);

                if ((object)test1 != null)
                {
                    Assert.Same(access != Accessibility.Private ? test1.GetMember(m1.Name) : null, test1.FindImplementationForInterfaceMember(m1));
                }
            }

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            ValidateProperty(GetSingleProperty(compilation2, "I1"), true, Accessibility.Internal, Accessibility.Public);
            ValidateProperty(GetSingleProperty(compilation2, "I3"), false, Accessibility.Private, Accessibility.Public);
            ValidateProperty(GetSingleProperty(compilation2, "I4"), false, Accessibility.Public, Accessibility.Private);
            ValidateProperty(GetSingleProperty(compilation2, "I5"), false, Accessibility.Private, Accessibility.Public);
            ValidateProperty(GetSingleProperty(compilation2, "I6"), false, Accessibility.Public, Accessibility.Private);

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation2.ToMetadataReference() }, 
                                                         options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate1);

            Validate1(compilation3.SourceModule);

            var compilation4 = CreateStandardCompilation(source2, new[] { compilation2.EmitToImageReference() }, 
                                                         options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation4.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation4, verify: false, symbolValidator: Validate1);

            Validate1(compilation4.SourceModule);
        }

        [Fact]
        public void PropertyModifiers_24()
        {
            var source1 =
@"
public interface I1
{
    int P1
    {
        get 
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        internal set 
        {
            System.Console.WriteLine(""set_P1"");
        }
    }

    void M2() {P1 = P1;}
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.M2();
    }
}
";
            ValidatePropertyModifiers_24(source1, source2);
        }

        private void ValidatePropertyModifiers_24(string source1, string source2)
        {
            var compilation1 = CreateStandardCompilation(source1 + source2, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            Validate1(compilation1.SourceModule);

            void Validate1(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = test1.Interfaces.Single();
                var p1 = GetSingleProperty(i1);
                var p1get = p1.GetMethod;
                var p1set = p1.SetMethod;

                ValidateProperty(p1);
                ValidateMethod(p1get, Accessibility.Public);
                ValidateMethod(p1set, Accessibility.Internal);
                Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
                Assert.Same(p1get, test1.FindImplementationForInterfaceMember(p1get));
                Assert.Same(p1set, test1.FindImplementationForInterfaceMember(p1set));
            }

            void ValidateProperty(PropertySymbol p1)
            {
                Assert.False(p1.IsAbstract);
                Assert.True(p1.IsVirtual);
                Assert.False(p1.IsSealed);
                Assert.False(p1.IsStatic);
                Assert.False(p1.IsExtern);
                Assert.False(p1.IsOverride);
                Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            }

            void ValidateMethod(MethodSymbol m1, Accessibility access)
            {
                Assert.False(m1.IsAbstract);
                Assert.True(m1.IsVirtual);
                Assert.True(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(access, m1.DeclaredAccessibility);
            }

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            {
                var i1 = compilation2.GetTypeByMetadataName("I1");
                var p1 = GetSingleProperty(i1);
                var p1get = p1.GetMethod;
                var p1set = p1.SetMethod;

                ValidateProperty(p1);
                ValidateMethod(p1get, Accessibility.Public);
                ValidateMethod(p1set, Accessibility.Internal);
            }

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation2.ToMetadataReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate1);

            Validate1(compilation3.SourceModule);

            var compilation4 = CreateStandardCompilation(source2, new[] { compilation2.EmitToImageReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation4.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation4, verify: false, symbolValidator: Validate1);

            Validate1(compilation4.SourceModule);
        }

        [Fact]
        public void PropertyModifiers_25()
        {
            var source1 =
@"
public interface I1
{
    static int P1 { private get => throw null; set => throw null; }

    static int P2 { internal get => throw null; set => throw null; }

    public static int P3 { get => throw null; private set => throw null; }

    static int P4 { get => throw null; internal set => throw null; }
}

class Test1
{
    static void Main()
    {
        int x;
        x = I1.P1;
        I1.P1 = x;
        x = I1.P2;
        I1.P2 = x;
        x = I1.P3;
        I1.P3 = x;
        x = I1.P4;
        I1.P4 = x;
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (18,13): error CS0271: The property or indexer 'I1.P1' cannot be used in this context because the get accessor is inaccessible
                //         x = I1.P1;
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "I1.P1").WithArguments("I1.P1").WithLocation(18, 13),
                // (23,9): error CS0272: The property or indexer 'I1.P3' cannot be used in this context because the set accessor is inaccessible
                //         I1.P3 = x;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "I1.P3").WithArguments("I1.P3").WithLocation(23, 9)
                );

            var source2 =
@"
class Test2
{
    static void Main()
    {
        int x;
        x = I1.P1;
        I1.P1 = x;
        x = I1.P2;
        I1.P2 = x;
        x = I1.P3;
        I1.P3 = x;
        x = I1.P4;
        I1.P4 = x;
    }
}
";
            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() },
                                                         options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(
                // (7,13): error CS0271: The property or indexer 'I1.P1' cannot be used in this context because the get accessor is inaccessible
                //         x = I1.P1;
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "I1.P1").WithArguments("I1.P1").WithLocation(7, 13),
                // (9,13): error CS0271: The property or indexer 'I1.P2' cannot be used in this context because the get accessor is inaccessible
                //         x = I1.P2;
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "I1.P2").WithArguments("I1.P2").WithLocation(9, 13),
                // (12,9): error CS0272: The property or indexer 'I1.P3' cannot be used in this context because the set accessor is inaccessible
                //         I1.P3 = x;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "I1.P3").WithArguments("I1.P3").WithLocation(12, 9),
                // (14,9): error CS0272: The property or indexer 'I1.P4' cannot be used in this context because the set accessor is inaccessible
                //         I1.P4 = x;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "I1.P4").WithArguments("I1.P4").WithLocation(14, 9)
                );
        }

        [Fact]
        public void PropertyModifiers_26()
        {
            var source1 =
@"
public interface I1
{
    abstract int P1 { private get; set; }
    abstract int P2 { get; private set; }
    abstract int P3 { internal get; }
    static int P4 {internal get;} = 0;
    static int P5 { internal get {throw null;} }
    static int P6 { internal set {throw null;} }
    static int P7 { internal get => throw null; }
    static int P8 { internal set => throw null; }
    static int P9 { internal get {throw null;} private set {throw null;}}
    static int P10 { internal get => throw null; private set => throw null;}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,31): error CS0442: 'I1.P1.get': abstract properties cannot have private accessors
                //     abstract int P1 { private get; set; }
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "get").WithArguments("I1.P1.get").WithLocation(4, 31),
                // (5,36): error CS0442: 'I1.P2.set': abstract properties cannot have private accessors
                //     abstract int P2 { get; private set; }
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "set").WithArguments("I1.P2.set").WithLocation(5, 36),
                // (6,18): error CS0276: 'I1.P3': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     abstract int P3 { internal get; }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "P3").WithArguments("I1.P3").WithLocation(6, 18),
                // (7,16): error CS8052: Auto-implemented properties inside interfaces cannot have initializers.
                //     static int P4 {internal get;} = 0;
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P4").WithArguments("I1.P4").WithLocation(7, 16),
                // (7,29): error CS0501: 'I1.P4.get' must declare a body because it is not marked abstract, extern, or partial
                //     static int P4 {internal get;} = 0;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.P4.get").WithLocation(7, 29),
                // (7,16): error CS0276: 'I1.P4': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     static int P4 {internal get;} = 0;
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "P4").WithArguments("I1.P4").WithLocation(7, 16),
                // (8,16): error CS0276: 'I1.P5': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     static int P5 { internal get {throw null;} }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "P5").WithArguments("I1.P5").WithLocation(8, 16),
                // (9,16): error CS0276: 'I1.P6': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     static int P6 { internal set {throw null;} }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "P6").WithArguments("I1.P6").WithLocation(9, 16),
                // (10,16): error CS0276: 'I1.P7': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     static int P7 { internal get => throw null; }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "P7").WithArguments("I1.P7").WithLocation(10, 16),
                // (11,16): error CS0276: 'I1.P8': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     static int P8 { internal set => throw null; }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "P8").WithArguments("I1.P8").WithLocation(11, 16),
                // (12,16): error CS0274: Cannot specify accessibility modifiers for both accessors of the property or indexer 'I1.P9'
                //     static int P9 { internal get {throw null;} private set {throw null;}}
                Diagnostic(ErrorCode.ERR_DuplicatePropertyAccessMods, "P9").WithArguments("I1.P9").WithLocation(12, 16),
                // (13,16): error CS0274: Cannot specify accessibility modifiers for both accessors of the property or indexer 'I1.P10'
                //     static int P10 { internal get => throw null; private set => throw null;}
                Diagnostic(ErrorCode.ERR_DuplicatePropertyAccessMods, "P10").WithArguments("I1.P10").WithLocation(13, 16)
                );
        }

        [Fact]
        public void PropertyModifiers_27()
        {
            var source1 =
@"
public interface I1
{
    int P3
    {
        private get {throw null;} 
        set {}
    }

    int P4
    {
        get {throw null;} 
        private set {}
    }
}

class Test1 : I1
{
    int I1.P3
    {
        get {throw null;} 
        set {}
    }

    int I1.P4
    {
        get {throw null;} 
        set {}
    }
}

class Test2 : I1
{
    int I1.P3
    {
        set {}
    }

    int I1.P4
    {
        get => throw null; 
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            // PROTOTYPE(DefaultInterfaceImplementation): The lack of errors for Test1 looks wrong. Private accessor is never virtual 
            //                                            (the behavior goes back to native compiler). So, it would be wrong
            //                                            to have a MethodImpl to point to an accessor like this in an interface. 
            compilation1.VerifyDiagnostics(
                // (34,12): error CS0551: Explicit interface implementation 'Test2.I1.P3' is missing accessor 'I1.P3.get'
                //     int I1.P3
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "P3").WithArguments("Test2.I1.P3", "I1.P3.get").WithLocation(34, 12),
                // (39,12): error CS0551: Explicit interface implementation 'Test2.I1.P4' is missing accessor 'I1.P4.set'
                //     int I1.P4
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "P4").WithArguments("Test2.I1.P4", "I1.P4.set").WithLocation(39, 12)
                );
        }

        [Fact]
        public void IndexerModifiers_01()
        {
            var source1 =
@"
public interface I01{ public int this[int x] {get; set;} }
public interface I02{ protected int this[int x] {get;} }
public interface I03{ protected internal int this[int x] {set;} }
public interface I04{ internal int this[int x] {get;} }
public interface I05{ private int this[int x] {set;} }
public interface I06{ static int this[int x] {get;} }
public interface I07{ virtual int this[int x] {set;} }
public interface I08{ sealed int this[int x] {get;} }
public interface I09{ override int this[int x] {set;} }
public interface I10{ abstract int this[int x] {get;} }
public interface I11{ extern int this[int x] {get; set;} }

public interface I12{ int this[int x] { public get; set;} }
public interface I13{ int this[int x] { get; protected set;} }
public interface I14{ int this[int x] { protected internal get; set;} }
public interface I15{ int this[int x] { get; internal set;} }
public interface I16{ int this[int x] { private get; set;} }
public interface I17{ int this[int x] { private get;} }
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (3,37): error CS0106: The modifier 'protected' is not valid for this item
                // public interface I02{ protected int this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("protected").WithLocation(3, 37),
                // (4,46): error CS0106: The modifier 'protected internal' is not valid for this item
                // public interface I03{ protected internal int this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("protected internal").WithLocation(4, 46),
                // (6,48): error CS0501: 'I05.this[int].set' must declare a body because it is not marked abstract, extern, or partial
                // public interface I05{ private int this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I05.this[int].set").WithLocation(6, 48),
                // (7,34): error CS0106: The modifier 'static' is not valid for this item
                // public interface I06{ static int this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(7, 34),
                // (8,48): error CS0501: 'I07.this[int].set' must declare a body because it is not marked abstract, extern, or partial
                // public interface I07{ virtual int this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I07.this[int].set").WithLocation(8, 48),
                // (9,47): error CS0501: 'I08.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                // public interface I08{ sealed int this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I08.this[int].get").WithLocation(9, 47),
                // (10,36): error CS0106: The modifier 'override' is not valid for this item
                // public interface I09{ override int this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("override").WithLocation(10, 36),
                // (14,48): error CS0273: The accessibility modifier of the 'I12.this[int].get' accessor must be more restrictive than the property or indexer 'I12.this[int]'
                // public interface I12{ int this[int x] { public get; set;} }
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("I12.this[int].get", "I12.this[int]").WithLocation(14, 48),
                // (15,56): error CS0106: The modifier 'protected' is not valid for this item
                // public interface I13{ int this[int x] { get; protected set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("protected").WithLocation(15, 56),
                // (16,60): error CS0106: The modifier 'protected internal' is not valid for this item
                // public interface I14{ int this[int x] { protected internal get; set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("protected internal").WithLocation(16, 60),
                // (18,49): error CS0442: 'I16.this[int].get': abstract properties cannot have private accessors
                // public interface I16{ int this[int x] { private get; set;} }
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "get").WithArguments("I16.this[int].get").WithLocation(18, 49),
                // (19,27): error CS0276: 'I17.this[int]': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                // public interface I17{ int this[int x] { private get;} }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "this").WithArguments("I17.this[int]").WithLocation(19, 27),
                // (12,47): warning CS0626: Method, operator, or accessor 'I11.this[int].get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                // public interface I11{ extern int this[int x] {get; set;} }
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("I11.this[int].get").WithLocation(12, 47),
                // (12,52): warning CS0626: Method, operator, or accessor 'I11.this[int].set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                // public interface I11{ extern int this[int x] {get; set;} }
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("I11.this[int].set").WithLocation(12, 52)
                );

            ValidateSymbolsIndexerModifiers_01(compilation1);
        }

        private static void ValidateSymbolsIndexerModifiers_01(CSharpCompilation compilation1)
        {
            var p01 = compilation1.GetMember<PropertySymbol>("I01.this[]");

            Assert.True(p01.IsAbstract);
            Assert.False(p01.IsVirtual);
            Assert.False(p01.IsSealed);
            Assert.False(p01.IsStatic);
            Assert.False(p01.IsExtern);
            Assert.False(p01.IsOverride);
            Assert.Equal(Accessibility.Public, p01.DeclaredAccessibility);

            VaidateP01Accessor(p01.GetMethod);
            VaidateP01Accessor(p01.SetMethod);
            void VaidateP01Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p02 = compilation1.GetMember<PropertySymbol>("I02.this[]");
            var p02get = p02.GetMethod;

            Assert.True(p02.IsAbstract);
            Assert.False(p02.IsVirtual);
            Assert.False(p02.IsSealed);
            Assert.False(p02.IsStatic);
            Assert.False(p02.IsExtern);
            Assert.False(p02.IsOverride);
            Assert.Equal(Accessibility.Public, p02.DeclaredAccessibility);

            Assert.True(p02get.IsAbstract);
            Assert.False(p02get.IsVirtual);
            Assert.True(p02get.IsMetadataVirtual());
            Assert.False(p02get.IsSealed);
            Assert.False(p02get.IsStatic);
            Assert.False(p02get.IsExtern);
            Assert.False(p02get.IsAsync);
            Assert.False(p02get.IsOverride);
            Assert.Equal(Accessibility.Public, p02get.DeclaredAccessibility);

            var p03 = compilation1.GetMember<PropertySymbol>("I03.this[]");
            var p03set = p03.SetMethod;

            Assert.True(p03.IsAbstract);
            Assert.False(p03.IsVirtual);
            Assert.False(p03.IsSealed);
            Assert.False(p03.IsStatic);
            Assert.False(p03.IsExtern);
            Assert.False(p03.IsOverride);
            Assert.Equal(Accessibility.Public, p03.DeclaredAccessibility);

            Assert.True(p03set.IsAbstract);
            Assert.False(p03set.IsVirtual);
            Assert.True(p03set.IsMetadataVirtual());
            Assert.False(p03set.IsSealed);
            Assert.False(p03set.IsStatic);
            Assert.False(p03set.IsExtern);
            Assert.False(p03set.IsAsync);
            Assert.False(p03set.IsOverride);
            Assert.Equal(Accessibility.Public, p03set.DeclaredAccessibility);

            var p04 = compilation1.GetMember<PropertySymbol>("I04.this[]");
            var p04get = p04.GetMethod;

            Assert.True(p04.IsAbstract);
            Assert.False(p04.IsVirtual);
            Assert.False(p04.IsSealed);
            Assert.False(p04.IsStatic);
            Assert.False(p04.IsExtern);
            Assert.False(p04.IsOverride);
            Assert.Equal(Accessibility.Internal, p04.DeclaredAccessibility);

            Assert.True(p04get.IsAbstract);
            Assert.False(p04get.IsVirtual);
            Assert.True(p04get.IsMetadataVirtual());
            Assert.False(p04get.IsSealed);
            Assert.False(p04get.IsStatic);
            Assert.False(p04get.IsExtern);
            Assert.False(p04get.IsAsync);
            Assert.False(p04get.IsOverride);
            Assert.Equal(Accessibility.Internal, p04get.DeclaredAccessibility);

            var p05 = compilation1.GetMember<PropertySymbol>("I05.this[]");
            var p05set = p05.SetMethod;

            Assert.False(p05.IsAbstract);
            Assert.False(p05.IsVirtual);
            Assert.False(p05.IsSealed);
            Assert.False(p05.IsStatic);
            Assert.False(p05.IsExtern);
            Assert.False(p05.IsOverride);
            Assert.Equal(Accessibility.Private, p05.DeclaredAccessibility);

            Assert.False(p05set.IsAbstract);
            Assert.False(p05set.IsVirtual);
            Assert.False(p05set.IsMetadataVirtual());
            Assert.False(p05set.IsSealed);
            Assert.False(p05set.IsStatic);
            Assert.False(p05set.IsExtern);
            Assert.False(p05set.IsAsync);
            Assert.False(p05set.IsOverride);
            Assert.Equal(Accessibility.Private, p05set.DeclaredAccessibility);

            var p06 = compilation1.GetMember<PropertySymbol>("I06.this[]");
            var p06get = p06.GetMethod;

            Assert.True(p06.IsAbstract);
            Assert.False(p06.IsVirtual);
            Assert.False(p06.IsSealed);
            Assert.False(p06.IsStatic);
            Assert.False(p06.IsExtern);
            Assert.False(p06.IsOverride);
            Assert.Equal(Accessibility.Public, p06.DeclaredAccessibility);

            Assert.True(p06get.IsAbstract);
            Assert.False(p06get.IsVirtual);
            Assert.True(p06get.IsMetadataVirtual());
            Assert.False(p06get.IsSealed);
            Assert.False(p06get.IsStatic);
            Assert.False(p06get.IsExtern);
            Assert.False(p06get.IsAsync);
            Assert.False(p06get.IsOverride);
            Assert.Equal(Accessibility.Public, p06get.DeclaredAccessibility);

            var p07 = compilation1.GetMember<PropertySymbol>("I07.this[]");
            var p07set = p07.SetMethod;

            Assert.False(p07.IsAbstract);
            Assert.True(p07.IsVirtual);
            Assert.False(p07.IsSealed);
            Assert.False(p07.IsStatic);
            Assert.False(p07.IsExtern);
            Assert.False(p07.IsOverride);
            Assert.Equal(Accessibility.Public, p07.DeclaredAccessibility);

            Assert.False(p07set.IsAbstract);
            Assert.True(p07set.IsVirtual);
            Assert.True(p07set.IsMetadataVirtual());
            Assert.False(p07set.IsSealed);
            Assert.False(p07set.IsStatic);
            Assert.False(p07set.IsExtern);
            Assert.False(p07set.IsAsync);
            Assert.False(p07set.IsOverride);
            Assert.Equal(Accessibility.Public, p07set.DeclaredAccessibility);

            var p08 = compilation1.GetMember<PropertySymbol>("I08.this[]");
            var p08get = p08.GetMethod;

            Assert.False(p08.IsAbstract);
            Assert.False(p08.IsVirtual);
            Assert.False(p08.IsSealed);
            Assert.False(p08.IsStatic);
            Assert.False(p08.IsExtern);
            Assert.False(p08.IsOverride);
            Assert.Equal(Accessibility.Public, p08.DeclaredAccessibility);

            Assert.False(p08get.IsAbstract);
            Assert.False(p08get.IsVirtual);
            Assert.False(p08get.IsMetadataVirtual());
            Assert.False(p08get.IsSealed);
            Assert.False(p08get.IsStatic);
            Assert.False(p08get.IsExtern);
            Assert.False(p08get.IsAsync);
            Assert.False(p08get.IsOverride);
            Assert.Equal(Accessibility.Public, p08get.DeclaredAccessibility);

            var p09 = compilation1.GetMember<PropertySymbol>("I09.this[]");
            var p09set = p09.SetMethod;

            Assert.True(p09.IsAbstract);
            Assert.False(p09.IsVirtual);
            Assert.False(p09.IsSealed);
            Assert.False(p09.IsStatic);
            Assert.False(p09.IsExtern);
            Assert.False(p09.IsOverride);
            Assert.Equal(Accessibility.Public, p09.DeclaredAccessibility);

            Assert.True(p09set.IsAbstract);
            Assert.False(p09set.IsVirtual);
            Assert.True(p09set.IsMetadataVirtual());
            Assert.False(p09set.IsSealed);
            Assert.False(p09set.IsStatic);
            Assert.False(p09set.IsExtern);
            Assert.False(p09set.IsAsync);
            Assert.False(p09set.IsOverride);
            Assert.Equal(Accessibility.Public, p09set.DeclaredAccessibility);

            var p10 = compilation1.GetMember<PropertySymbol>("I10.this[]");
            var p10get = p10.GetMethod;

            Assert.True(p10.IsAbstract);
            Assert.False(p10.IsVirtual);
            Assert.False(p10.IsSealed);
            Assert.False(p10.IsStatic);
            Assert.False(p10.IsExtern);
            Assert.False(p10.IsOverride);
            Assert.Equal(Accessibility.Public, p10.DeclaredAccessibility);

            Assert.True(p10get.IsAbstract);
            Assert.False(p10get.IsVirtual);
            Assert.True(p10get.IsMetadataVirtual());
            Assert.False(p10get.IsSealed);
            Assert.False(p10get.IsStatic);
            Assert.False(p10get.IsExtern);
            Assert.False(p10get.IsAsync);
            Assert.False(p10get.IsOverride);
            Assert.Equal(Accessibility.Public, p10get.DeclaredAccessibility);

            var p11 = compilation1.GetMember<PropertySymbol>("I11.this[]");

            Assert.False(p11.IsAbstract);
            Assert.True(p11.IsVirtual);
            Assert.False(p11.IsSealed);
            Assert.False(p11.IsStatic);
            Assert.True(p11.IsExtern);
            Assert.False(p11.IsOverride);
            Assert.Equal(Accessibility.Public, p11.DeclaredAccessibility);

            ValidateP11Accessor(p11.GetMethod);
            ValidateP11Accessor(p11.SetMethod);
            void ValidateP11Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.True(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.True(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p12 = compilation1.GetMember<PropertySymbol>("I12.this[]");

            Assert.True(p12.IsAbstract);
            Assert.False(p12.IsVirtual);
            Assert.False(p12.IsSealed);
            Assert.False(p12.IsStatic);
            Assert.False(p12.IsExtern);
            Assert.False(p12.IsOverride);
            Assert.Equal(Accessibility.Public, p12.DeclaredAccessibility);

            ValidateP12Accessor(p12.GetMethod);
            ValidateP12Accessor(p12.SetMethod);
            void ValidateP12Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p13 = compilation1.GetMember<PropertySymbol>("I13.this[]");

            Assert.True(p13.IsAbstract);
            Assert.False(p13.IsVirtual);
            Assert.False(p13.IsSealed);
            Assert.False(p13.IsStatic);
            Assert.False(p13.IsExtern);
            Assert.False(p13.IsOverride);
            Assert.Equal(Accessibility.Public, p13.DeclaredAccessibility);

            ValidateP13Accessor(p13.GetMethod);
            ValidateP13Accessor(p13.SetMethod);
            void ValidateP13Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p14 = compilation1.GetMember<PropertySymbol>("I14.this[]");

            Assert.True(p14.IsAbstract);
            Assert.False(p14.IsVirtual);
            Assert.False(p14.IsSealed);
            Assert.False(p14.IsStatic);
            Assert.False(p14.IsExtern);
            Assert.False(p14.IsOverride);
            Assert.Equal(Accessibility.Public, p14.DeclaredAccessibility);

            ValidateP14Accessor(p14.GetMethod);
            ValidateP14Accessor(p14.SetMethod);
            void ValidateP14Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p15 = compilation1.GetMember<PropertySymbol>("I15.this[]");

            Assert.True(p15.IsAbstract);
            Assert.False(p15.IsVirtual);
            Assert.False(p15.IsSealed);
            Assert.False(p15.IsStatic);
            Assert.False(p15.IsExtern);
            Assert.False(p15.IsOverride);
            Assert.Equal(Accessibility.Public, p15.DeclaredAccessibility);

            ValidateP15Accessor(p15.GetMethod, Accessibility.Public);
            ValidateP15Accessor(p15.SetMethod, Accessibility.Internal);
            void ValidateP15Accessor(MethodSymbol accessor, Accessibility accessibility)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(accessibility, accessor.DeclaredAccessibility);
            }

            var p16 = compilation1.GetMember<PropertySymbol>("I16.this[]");

            Assert.True(p16.IsAbstract);
            Assert.False(p16.IsVirtual);
            Assert.False(p16.IsSealed);
            Assert.False(p16.IsStatic);
            Assert.False(p16.IsExtern);
            Assert.False(p16.IsOverride);
            Assert.Equal(Accessibility.Public, p16.DeclaredAccessibility);

            ValidateP16Accessor(p16.GetMethod, Accessibility.Private);
            ValidateP16Accessor(p16.SetMethod, Accessibility.Public);
            void ValidateP16Accessor(MethodSymbol accessor, Accessibility accessibility)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(accessibility, accessor.DeclaredAccessibility);
            }

            var p17 = compilation1.GetMember<PropertySymbol>("I17.this[]");
            var p17get = p17.GetMethod;

            Assert.True(p17.IsAbstract);
            Assert.False(p17.IsVirtual);
            Assert.False(p17.IsSealed);
            Assert.False(p17.IsStatic);
            Assert.False(p17.IsExtern);
            Assert.False(p17.IsOverride);
            Assert.Equal(Accessibility.Public, p17.DeclaredAccessibility);

            Assert.True(p17get.IsAbstract);
            Assert.False(p17get.IsVirtual);
            Assert.True(p17get.IsMetadataVirtual());
            Assert.False(p17get.IsSealed);
            Assert.False(p17get.IsStatic);
            Assert.False(p17get.IsExtern);
            Assert.False(p17get.IsAsync);
            Assert.False(p17get.IsOverride);
            Assert.Equal(Accessibility.Private, p17get.DeclaredAccessibility);
        }

        [Fact]
        public void IndexerModifiers_02()
        {
            var source1 =
@"
public interface I01{ public int this[int x] {get; set;} }
public interface I02{ protected int this[int x] {get;} }
public interface I03{ protected internal int this[int x] {set;} }
public interface I04{ internal int this[int x] {get;} }
public interface I05{ private int this[int x] {set;} }
public interface I06{ static int this[int x] {get;} }
public interface I07{ virtual int this[int x] {set;} }
public interface I08{ sealed int this[int x] {get;} }
public interface I09{ override int this[int x] {set;} }
public interface I10{ abstract int this[int x] {get;} }
public interface I11{ extern int this[int x] {get; set;} }

public interface I12{ int this[int x] { public get; set;} }
public interface I13{ int this[int x] { get; protected set;} }
public interface I14{ int this[int x] { protected internal get; set;} }
public interface I15{ int this[int x] { get; internal set;} }
public interface I16{ int this[int x] { private get; set;} }
public interface I17{ int this[int x] { private get;} }
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (2,34): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I01{ public int this[int x] {get; set;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "this").WithArguments("public", "7", "7.1").WithLocation(2, 34),
                // (3,37): error CS0106: The modifier 'protected' is not valid for this item
                // public interface I02{ protected int this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("protected").WithLocation(3, 37),
                // (4,46): error CS0106: The modifier 'protected internal' is not valid for this item
                // public interface I03{ protected internal int this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("protected internal").WithLocation(4, 46),
                // (5,36): error CS8503: The modifier 'internal' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I04{ internal int this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "this").WithArguments("internal", "7", "7.1").WithLocation(5, 36),
                // (6,35): error CS8503: The modifier 'private' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I05{ private int this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "this").WithArguments("private", "7", "7.1").WithLocation(6, 35),
                // (6,48): error CS0501: 'I05.this[int].set' must declare a body because it is not marked abstract, extern, or partial
                // public interface I05{ private int this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I05.this[int].set").WithLocation(6, 48),
                // (7,34): error CS0106: The modifier 'static' is not valid for this item
                // public interface I06{ static int this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(7, 34),
                // (8,35): error CS8503: The modifier 'virtual' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I07{ virtual int this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "this").WithArguments("virtual", "7", "7.1").WithLocation(8, 35),
                // (8,48): error CS0501: 'I07.this[int].set' must declare a body because it is not marked abstract, extern, or partial
                // public interface I07{ virtual int this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I07.this[int].set").WithLocation(8, 48),
                // (9,34): error CS8503: The modifier 'sealed' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I08{ sealed int this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "this").WithArguments("sealed", "7", "7.1").WithLocation(9, 34),
                // (9,47): error CS0501: 'I08.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                // public interface I08{ sealed int this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I08.this[int].get").WithLocation(9, 47),
                // (10,36): error CS0106: The modifier 'override' is not valid for this item
                // public interface I09{ override int this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("override").WithLocation(10, 36),
                // (11,36): error CS8503: The modifier 'abstract' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I10{ abstract int this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "this").WithArguments("abstract", "7", "7.1").WithLocation(11, 36),
                // (12,34): error CS8503: The modifier 'extern' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I11{ extern int this[int x] {get; set;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "this").WithArguments("extern", "7", "7.1").WithLocation(12, 34),
                // (14,48): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I12{ int this[int x] { public get; set;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "get").WithArguments("public", "7", "7.1").WithLocation(14, 48),
                // (14,48): error CS0273: The accessibility modifier of the 'I12.this[int].get' accessor must be more restrictive than the property or indexer 'I12.this[int]'
                // public interface I12{ int this[int x] { public get; set;} }
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("I12.this[int].get", "I12.this[int]").WithLocation(14, 48),
                // (15,56): error CS0106: The modifier 'protected' is not valid for this item
                // public interface I13{ int this[int x] { get; protected set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("protected").WithLocation(15, 56),
                // (16,60): error CS0106: The modifier 'protected internal' is not valid for this item
                // public interface I14{ int this[int x] { protected internal get; set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("protected internal").WithLocation(16, 60),
                // (17,55): error CS8503: The modifier 'internal' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I15{ int this[int x] { get; internal set;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "set").WithArguments("internal", "7", "7.1").WithLocation(17, 55),
                // (18,49): error CS8503: The modifier 'private' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I16{ int this[int x] { private get; set;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "get").WithArguments("private", "7", "7.1").WithLocation(18, 49),
                // (18,49): error CS0442: 'I16.this[int].get': abstract properties cannot have private accessors
                // public interface I16{ int this[int x] { private get; set;} }
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "get").WithArguments("I16.this[int].get").WithLocation(18, 49),
                // (19,49): error CS8503: The modifier 'private' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                // public interface I17{ int this[int x] { private get;} }
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "get").WithArguments("private", "7", "7.1").WithLocation(19, 49),
                // (19,27): error CS0276: 'I17.this[int]': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                // public interface I17{ int this[int x] { private get;} }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "this").WithArguments("I17.this[int]").WithLocation(19, 27),
                // (12,47): warning CS0626: Method, operator, or accessor 'I11.this[int].get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                // public interface I11{ extern int this[int x] {get; set;} }
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("I11.this[int].get").WithLocation(12, 47),
                // (12,52): warning CS0626: Method, operator, or accessor 'I11.this[int].set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                // public interface I11{ extern int this[int x] {get; set;} }
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("I11.this[int].set").WithLocation(12, 52)
                );

            ValidateSymbolsIndexerModifiers_01(compilation1);
        }

        [Fact]
        public void IndexerModifiers_03()
        {
            ValidateIndexerImplementation_101(@"
public interface I1
{
    public virtual int this[int i] 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
    }
}

class Test1 : I1
{}
");

            ValidateIndexerImplementation_101(@"
public interface I1
{
    public virtual int this[int i] 
    {
        get => Test1.GetP1();
    }
}

class Test1 : I1
{
    public static int GetP1()
    {
        System.Console.WriteLine(""get P1"");
        return 0;
    }
}
");

            ValidateIndexerImplementation_101(@"
public interface I1
{
    public virtual int this[int i] => Test1.GetP1(); 
}

class Test1 : I1
{
    public static int GetP1()
    {
        System.Console.WriteLine(""get P1"");
        return 0;
    }
}
");

            ValidateIndexerImplementation_102(@"
public interface I1
{
    public virtual int this[int i] 
    {
        get
        {
            System.Console.WriteLine(""get P1"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
");

            ValidateIndexerImplementation_102(@"
public interface I1
{
    public virtual int this[int i] 
    {
        get => Test1.GetP1();
        set => System.Console.WriteLine(""set P1"");
    }
}

class Test1 : I1
{
    public static int GetP1()
    {
        System.Console.WriteLine(""get P1"");
        return 0;
    }
}
");

            ValidateIndexerImplementation_103(@"
public interface I1
{
    public virtual int this[int i] 
    {
        set
        {
            System.Console.WriteLine(""set P1"");
        }
    }
}

class Test1 : I1
{}
");

            ValidateIndexerImplementation_103(@"
public interface I1
{
    public virtual int this[int i] 
    {
        set => System.Console.WriteLine(""set P1"");
    }
}

class Test1 : I1
{}
");
        }

        [Fact]
        public void IndexerModifiers_04()
        {
            var source1 =
@"
public interface I1
{
    public virtual int this[int x] { get; } = 0; 
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,45): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     public virtual int this[int x] { get; } = 0; 
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(4, 45),
                // (4,38): error CS0501: 'I1.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                //     public virtual int this[int x] { get; } = 0; 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.this[int].get").WithLocation(4, 38)
                );

            ValidatePropertyModifiers_04(compilation1, "this[]");
        }

        [Fact]
        public void IndexerModifiers_05()
        {
            var source1 =
@"
public interface I1
{
    public abstract int this[int x] {get; set;} 
}
public interface I2
{
    int this[int x] {get; set;} 
}

class Test1 : I1
{
    public int this[int x] 
    {
        get
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set => System.Console.WriteLine(""set_P1"");
    }
}
class Test2 : I2
{
    public int this[int x] 
    {
        get
        {
            System.Console.WriteLine(""get_P2"");
            return 0;
        }
        set => System.Console.WriteLine(""set_P2"");
    }

    static void Main()
    {
        I1 x = new Test1();
        x[0] = x[0];
        I2 y = new Test2();
        y[0] = y[0];
    }
}
";

            ValidatePropertyModifiers_05(source1);
        }

        [Fact]
        public void IndexerModifiers_06()
        {
            var source1 =
@"
public interface I1
{
    public abstract int this[int x] {get; set;} 
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,25): error CS8503: The modifier 'abstract' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public abstract int this[int x] {get; set;} 
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "this").WithArguments("abstract", "7", "7.1").WithLocation(4, 25),
                // (4,25): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public abstract int this[int x] {get; set;} 
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "this").WithArguments("public", "7", "7.1").WithLocation(4, 25)
                );

            ValidatePropertyModifiers_06(compilation1, "this[]");
        }

        [Fact]
        public void IndexerModifiers_09()
        {
            var source1 =
@"
public interface I1
{
    private int this[int x]
    {
        get
        { 
            System.Console.WriteLine(""get_P1"");
            return 0;
        }           
    }
    sealed void M()
    {
        var x = this[0];
    }
}
public interface I2
{
    private int this[int x] 
    {
        get
        { 
            System.Console.WriteLine(""get_P2"");
            return 0;
        }           
        set
        { 
            System.Console.WriteLine(""set_P2"");
        }           
    }
    sealed void M()
    {
        this[0] = this[0];
    }
}
public interface I3
{
    private int this[int x] 
    {
        set
        { 
            System.Console.WriteLine(""set_P3"");
        }           
    }
    sealed void M()
    {
        this[0] = 0;
    }
}
public interface I4
{
    private int this[int x] 
    {
        get => GetP4();
    }

    private int GetP4()
    { 
        System.Console.WriteLine(""get_P4"");
        return 0;
    }           
    sealed void M()
    {
        var x = this[0];
    }
}
public interface I5
{
    private int this[int x] 
    {
        get => GetP5();
        set => System.Console.WriteLine(""set_P5"");
    }

    private int GetP5()
    { 
        System.Console.WriteLine(""get_P5"");
        return 0;
    }           
    sealed void M()
    {
        this[0] = this[0];
    }
}
public interface I6
{
    private int this[int x] 
    {
        set => System.Console.WriteLine(""set_P6"");
    }
    sealed void M()
    {
        this[0] = 0;
    }
}
public interface I7
{
    private int this[int x] => GetP7();

    private int GetP7()
    { 
        System.Console.WriteLine(""get_P7"");
        return 0;
    }           
    sealed void M()
    {
        var x = this[0];
    }
}

class Test1 : I1, I2, I3, I4, I5, I6, I7
{
    static void Main()
    {
        I1 x1 = new Test1();
        x1.M();
        I2 x2 = new Test1();
        x2.M();
        I3 x3 = new Test1();
        x3.M();
        I4 x4 = new Test1();
        x4.M();
        I5 x5 = new Test1();
        x5.M();
        I6 x6 = new Test1();
        x6.M();
        I7 x7 = new Test1();
        x7.M();
    }
}
";

            ValidatePropertyModifiers_09(source1);
        }

        [Fact]
        public void IndexerModifiers_10()
        {
            var source1 =
@"
public interface I1
{
    abstract private int this[byte x] { get; } 

    virtual private int this[int x] => 0;

    sealed private int this[short x] 
    {
        get => 0;
        set {}
    }

    private int this[long x] {get;} = 0;
}

class Test1 : I1
{
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (14,37): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     private int this[long x] {get;} = 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(14, 37),
                // (4,26): error CS0621: 'I1.this[byte]': virtual or abstract members cannot be private
                //     abstract private int this[byte x] { get; } 
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "this").WithArguments("I1.this[byte]").WithLocation(4, 26),
                // (6,25): error CS0621: 'I1.this[int]': virtual or abstract members cannot be private
                //     virtual private int this[int x] => 0;
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "this").WithArguments("I1.this[int]").WithLocation(6, 25),
                // (8,24): error CS0238: 'I1.this[short]' cannot be sealed because it is not an override
                //     sealed private int this[short x] 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "this").WithArguments("I1.this[short]").WithLocation(8, 24),
                // (14,31): error CS0501: 'I1.this[long].get' must declare a body because it is not marked abstract, extern, or partial
                //     private int this[long x] {get;} = 0;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.this[long].get").WithLocation(14, 31),
                // (17,15): error CS0535: 'Test1' does not implement interface member 'I1.this[byte]'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.this[byte]")
                );

            ValidatePropertyModifiers_10(compilation1);
        }

        [Fact]
        public void IndexerModifiers_11()
        {
            var source1 =
@"
public interface I1
{
    internal abstract int this[int x] {get; set;} 

    sealed void Test()
    {
        this[0] = this[0];
    }
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.Test();
    }

    public int this[int x] 
    {
        get
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""set_P1"");
        }
    }
}
";

            ValidatePropertyModifiers_11(source1, source2,
                // (2,15): error CS0535: 'Test2' does not implement interface member 'I1.this[int]'
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.this[int]")
                );
        }

        [Fact]
        public void IndexerModifiers_12()
        {
            var source1 =
@"
public interface I1
{
    internal abstract int this[int x] {get; set;} 
}

class Test1 : I1
{
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (7,15): error CS0535: 'Test1' does not implement interface member 'I1.this[int]'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.this[int]")
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p1 = i1.GetMember<PropertySymbol>("this[]");
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));
            Assert.Null(test1.FindImplementationForInterfaceMember(p1.GetMethod));
            Assert.Null(test1.FindImplementationForInterfaceMember(p1.SetMethod));
        }

        [Fact]
        public void IndexerModifiers_13()
        {
            var source1 =
@"
public interface I1
{
    public sealed int this[int x]
    {
        get
        { 
            System.Console.WriteLine(""get_P1"");
            return 0;
        }           
    }
}
public interface I2
{
    public sealed int this[int x] 
    {
        get
        { 
            System.Console.WriteLine(""get_P2"");
            return 0;
        }           
        set
        { 
            System.Console.WriteLine(""set_P2"");
        }           
    }
}
public interface I3
{
    public sealed int this[int x] 
    {
        set
        { 
            System.Console.WriteLine(""set_P3"");
        }           
    }
}
public interface I4
{
    public sealed int this[int x] 
    {
        get => GetP4();
    }

    private int GetP4()
    { 
        System.Console.WriteLine(""get_P4"");
        return 0;
    }           
}
public interface I5
{
    public sealed int this[int x] 
    {
        get => GetP5();
        set => System.Console.WriteLine(""set_P5"");
    }

    private int GetP5()
    { 
        System.Console.WriteLine(""get_P5"");
        return 0;
    }           
}
public interface I6
{
    public sealed int this[int x] 
    {
        set => System.Console.WriteLine(""set_P6"");
    }
}
public interface I7
{
    public sealed int this[int x] => GetP7();

    private int GetP7()
    { 
        System.Console.WriteLine(""get_P7"");
        return 0;
    }           
}

class Test1 : I1
{
    static void Main()
    {
        I1 i1 = new Test1();
        var x = i1[0];
        I2 i2 = new Test2();
        i2[0] = i2[0];
        I3 i3 = new Test3();
        i3[0] = x;
        I4 i4 = new Test4();
        x = i4[0];
        I5 i5 = new Test5();
        i5[0] = i5[0];
        I6 i6 = new Test6();
        i6[0] = x;
        I7 i7 = new Test7();
        x = i7[0];
    }

    public int this[int x] => throw null;
}
class Test2 : I2
{
    public int this[int x] 
    {
        get => throw null;          
        set => throw null;         
    }
}
class Test3 : I3
{
    public int this[int x] 
    {
        set => throw null;      
    }
}
class Test4 : I4
{
    public int this[int x] 
    {
        get => throw null;
    }
}
class Test5 : I5
{
    public int this[int x] 
    {
        get => throw null;
        set => throw null;
    }
}
class Test6 : I6
{
    public int this[int x] 
    {
        set => throw null;
    }
}
class Test7 : I7
{
    public int this[int x] => throw null;
}
";

            ValidatePropertyModifiers_13(source1);
        }

        [Fact]
        public void AccessModifiers_14()
        {
            var source1 =
@"
public interface I1
{
    public sealed int this[int x] {get;} = 0; 
}
public interface I2
{
    abstract sealed int this[int x] {get;} 
}
public interface I3
{
    virtual sealed int this[int x]
    {
        set {}
    }
}

class Test1 : I1, I2, I3
{
    int I1.this[int x] { get => throw null; }
    int I2.this[int x] { get => throw null; }
    int I3.this[int x] { set => throw null; }
}

class Test2 : I1, I2, I3
{}
";
            ValidatePropertyModifiers_14(source1,
                // (4,42): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     public sealed int this[int x] {get;} = 0; 
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(4, 42),
                // (4,36): error CS0501: 'I1.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                //     public sealed int this[int x] {get;} = 0; 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I1.this[int].get").WithLocation(4, 36),
                // (8,25): error CS0238: 'I2.this[int]' cannot be sealed because it is not an override
                //     abstract sealed int this[int x] {get;} 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "this").WithArguments("I2.this[int]").WithLocation(8, 25),
                // (12,24): error CS0238: 'I3.this[int]' cannot be sealed because it is not an override
                //     virtual sealed int this[int x]
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "this").WithArguments("I3.this[int]").WithLocation(12, 24),
                // (20,12): error CS0539: 'Test1.this[int]' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I1.this[int x] { get => throw null; }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "this").WithArguments("Test1.this[int]").WithLocation(20, 12),
                // (25,19): error CS0535: 'Test2' does not implement interface member 'I2.this[int]'
                // class Test2 : I1, I2, I3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Test2", "I2.this[int]")
                );
        }

        [Fact]
        public void IndexerModifiers_15()
        {
            var source1 =
@"
public interface I0
{
    abstract virtual int this[int x] { get; set; }
}
public interface I1
{
    abstract virtual int this[int x] { get { throw null; } }
}
public interface I2
{
    virtual abstract int this[int x] 
    {
        get { throw null; }
        set { throw null; }
    }
}
public interface I3
{
    abstract virtual int this[int x] { set { throw null; } }
}
public interface I4
{
    abstract virtual int this[int x] { get => throw null; }
}
public interface I5
{
    abstract virtual int this[int x] 
    {
        get => throw null;
        set => throw null;
    }
}
public interface I6
{
    abstract virtual int this[int x] { set => throw null; }
}
public interface I7
{
    abstract virtual int this[int x] => throw null;
}
public interface I8
{
    abstract virtual int this[int x] {get;} = 0;
}

class Test1 : I0, I1, I2, I3, I4, I5, I6, I7, I8
{
    int I0.this[int x] 
    {
        get { throw null; }
        set { throw null; }
    }
    int I1.this[int x] 
    {
        get { throw null; }
    }
    int I2.this[int x] 
    {
        get { throw null; }
        set { throw null; }
    }
    int I3.this[int x] 
    {
        set { throw null; }
    }
    int I4.this[int x] 
    {
        get { throw null; }
    }
    int I5.this[int x] 
    {
        get { throw null; }
        set { throw null; }
    }
    int I6.this[int x] 
    {
        set { throw null; }
    }
    int I7.this[int x] 
    {
        get { throw null; }
    }
    int I8.this[int x] 
    {
        get { throw null; }
    }
}

class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
{}
";
            ValidatePropertyModifiers_15(source1,
                // (44,45): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     abstract virtual int this[int x] {get;} = 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(44, 45),
                // (4,26): error CS0503: The abstract method 'I0.this[int]' cannot be marked virtual
                //     abstract virtual int this[int x] { get; set; }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "this").WithArguments("I0.this[int]").WithLocation(4, 26),
                // (8,26): error CS0503: The abstract method 'I1.this[int]' cannot be marked virtual
                //     abstract virtual int this[int x] { get { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "this").WithArguments("I1.this[int]").WithLocation(8, 26),
                // (8,40): error CS0500: 'I1.this[int].get' cannot declare a body because it is marked abstract
                //     abstract virtual int this[int x] { get { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.this[int].get").WithLocation(8, 40),
                // (12,26): error CS0503: The abstract method 'I2.this[int]' cannot be marked virtual
                //     virtual abstract int this[int x] 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "this").WithArguments("I2.this[int]").WithLocation(12, 26),
                // (14,9): error CS0500: 'I2.this[int].get' cannot declare a body because it is marked abstract
                //         get { throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I2.this[int].get").WithLocation(14, 9),
                // (15,9): error CS0500: 'I2.this[int].set' cannot declare a body because it is marked abstract
                //         set { throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I2.this[int].set").WithLocation(15, 9),
                // (20,26): error CS0503: The abstract method 'I3.this[int]' cannot be marked virtual
                //     abstract virtual int this[int x] { set { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "this").WithArguments("I3.this[int]").WithLocation(20, 26),
                // (20,40): error CS0500: 'I3.this[int].set' cannot declare a body because it is marked abstract
                //     abstract virtual int this[int x] { set { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I3.this[int].set").WithLocation(20, 40),
                // (24,26): error CS0503: The abstract method 'I4.this[int]' cannot be marked virtual
                //     abstract virtual int this[int x] { get => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "this").WithArguments("I4.this[int]").WithLocation(24, 26),
                // (24,40): error CS0500: 'I4.this[int].get' cannot declare a body because it is marked abstract
                //     abstract virtual int this[int x] { get => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I4.this[int].get").WithLocation(24, 40),
                // (28,26): error CS0503: The abstract method 'I5.this[int]' cannot be marked virtual
                //     abstract virtual int this[int x] 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "this").WithArguments("I5.this[int]").WithLocation(28, 26),
                // (30,9): error CS0500: 'I5.this[int].get' cannot declare a body because it is marked abstract
                //         get => throw null;
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I5.this[int].get"),
                // (31,9): error CS0500: 'I5.this[int].set' cannot declare a body because it is marked abstract
                //         set => throw null;
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I5.this[int].set").WithLocation(31, 9),
                // (36,26): error CS0503: The abstract method 'I6.this[int]' cannot be marked virtual
                //     abstract virtual int this[int x] { set => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "this").WithArguments("I6.this[int]").WithLocation(36, 26),
                // (36,40): error CS0500: 'I6.this[int].set' cannot declare a body because it is marked abstract
                //     abstract virtual int this[int x] { set => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I6.this[int].set").WithLocation(36, 40),
                // (40,26): error CS0503: The abstract method 'I7.this[int]' cannot be marked virtual
                //     abstract virtual int this[int x] => throw null;
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "this").WithArguments("I7.this[int]").WithLocation(40, 26),
                // (40,41): error CS0500: 'I7.this[int].get' cannot declare a body because it is marked abstract
                //     abstract virtual int this[int x] => throw null;
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "throw null").WithArguments("I7.this[int].get").WithLocation(40, 41),
                // (44,26): error CS0503: The abstract method 'I8.this[int]' cannot be marked virtual
                //     abstract virtual int this[int x] {get;} = 0;
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "this").WithArguments("I8.this[int]").WithLocation(44, 26),
                // (90,15): error CS0535: 'Test2' does not implement interface member 'I0.this[int]'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I0").WithArguments("Test2", "I0.this[int]"),
                // (90,19): error CS0535: 'Test2' does not implement interface member 'I1.this[int]'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.this[int]"),
                // (90,23): error CS0535: 'Test2' does not implement interface member 'I2.this[int]'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Test2", "I2.this[int]"),
                // (90,27): error CS0535: 'Test2' does not implement interface member 'I3.this[int]'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I3").WithArguments("Test2", "I3.this[int]"),
                // (90,31): error CS0535: 'Test2' does not implement interface member 'I4.this[int]'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I4").WithArguments("Test2", "I4.this[int]"),
                // (90,35): error CS0535: 'Test2' does not implement interface member 'I5.this[int]'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I5").WithArguments("Test2", "I5.this[int]"),
                // (90,39): error CS0535: 'Test2' does not implement interface member 'I6.this[int]'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I6").WithArguments("Test2", "I6.this[int]"),
                // (90,43): error CS0535: 'Test2' does not implement interface member 'I7.this[int]'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I7").WithArguments("Test2", "I7.this[int]"),
                // (90,47): error CS0535: 'Test2' does not implement interface member 'I8.this[int]'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I8").WithArguments("Test2", "I8.this[int]")
                );
        }

        [Fact]
        public void IndexerModifiers_16()
        {
            var source1 =
@"
public interface I1
{
    extern int this[int x] {get;} 
}
public interface I2
{
    virtual extern int this[int x] {set;}
}
public interface I4
{
    private extern int this[int x] {get;}
}
public interface I5
{
    extern sealed int this[int x] {set;}
}

class Test1 : I1, I2, I4, I5
{
}

class Test2 : I1, I2, I4, I5
{
    int I1.this[int x] => 0;
    int I2.this[int x] { set {} }
}
";
            ValidatePropertyModifiers_16(source1);
        }

        [Fact]
        public void IndexerModifiers_17()
        {
            var source1 =
@"
public interface I1
{
    abstract extern int this[int x] {get;} 
}
public interface I2
{
    extern int this[int x] => 0; 
}
public interface I3
{
    static extern int this[int x] {get => 0; set => throw null;} 
}
public interface I4
{
    private extern int this[int x] { get {throw null;} set {throw null;}}
}
public interface I5
{
    extern sealed int this[int x] {get;} = 0;
}

class Test1 : I1, I2, I3, I4, I5
{
}

class Test2 : I1, I2, I3, I4, I5
{
    int I1.this[int x] => 0;
    int I2.this[int x] => 0;
    int I3.this[int x] { get => 0; set => throw null;}
    int I4.this[int x] { get => 0; set => throw null;}
    int I5.this[int x] => 0;
}
";
            ValidatePropertyModifiers_17(source1,
                // (20,42): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     extern sealed int this[int x] {get;} = 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(20, 42),
                // (4,25): error CS0180: 'I1.this[int]' cannot be both extern and abstract
                //     abstract extern int this[int x] {get;} 
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "this").WithArguments("I1.this[int]").WithLocation(4, 25),
                // (8,31): error CS0179: 'I2.this[int].get' cannot be extern and declare a body
                //     extern int this[int x] => 0; 
                Diagnostic(ErrorCode.ERR_ExternHasBody, "0").WithArguments("I2.this[int].get").WithLocation(8, 31),
                // (12,23): error CS0106: The modifier 'static' is not valid for this item
                //     static extern int this[int x] {get => 0; set => throw null;} 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(12, 23),
                // (12,36): error CS0179: 'I3.this[int].get' cannot be extern and declare a body
                //     static extern int this[int x] {get => 0; set => throw null;} 
                Diagnostic(ErrorCode.ERR_ExternHasBody, "get").WithArguments("I3.this[int].get").WithLocation(12, 36),
                // (12,46): error CS0179: 'I3.this[int].set' cannot be extern and declare a body
                //     static extern int this[int x] {get => 0; set => throw null;} 
                Diagnostic(ErrorCode.ERR_ExternHasBody, "set").WithArguments("I3.this[int].set").WithLocation(12, 46),
                // (16,38): error CS0179: 'I4.this[int].get' cannot be extern and declare a body
                //     private extern int this[int x] { get {throw null;} set {throw null;}}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "get").WithArguments("I4.this[int].get").WithLocation(16, 38),
                // (16,56): error CS0179: 'I4.this[int].set' cannot be extern and declare a body
                //     private extern int this[int x] { get {throw null;} set {throw null;}}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "set").WithArguments("I4.this[int].set").WithLocation(16, 56),
                // (23,15): error CS0535: 'Test1' does not implement interface member 'I1.this[int]'
                // class Test1 : I1, I2, I3, I4, I5
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.this[int]"),
                // (32,12): error CS0539: 'Test2.this[int]' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I4.this[int x] { get => 0; set => throw null;}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "this").WithArguments("Test2.this[int]").WithLocation(32, 12),
                // (33,12): error CS0539: 'Test2.this[int]' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I5.this[int x] => 0;
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "this").WithArguments("Test2.this[int]").WithLocation(33, 12),
                // (20,36): warning CS0626: Method, operator, or accessor 'I5.this[int].get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern sealed int this[int x] {get;} = 0;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("I5.this[int].get").WithLocation(20, 36)
                );
        }

        [Fact]
        public void IndexerModifiers_18()
        {
            var source1 =
@"
public interface I1
{
    abstract int this[int x] {get => 0; set => throw null;} 
}
public interface I2
{
    abstract private int this[int x] => 0; 
}
public interface I3
{
    static extern int this[int x] {get; set;} 
}
public interface I4
{
    abstract static int this[int x] { get {throw null;} set {throw null;}}
}
public interface I5
{
    override sealed int this[int x] {get;} = 0;
}

class Test1 : I1, I2, I3, I4, I5
{
}

class Test2 : I1, I2, I3, I4, I5
{
    int I1.this[int x] { get => 0; set => throw null;}
    int I2.this[int x] => 0;
    int I3.this[int x] { get => 0; set => throw null;}
    int I4.this[int x] { get => 0; set => throw null;}
    int I5.this[int x] => 0;
}
";
            ValidatePropertyModifiers_18(source1,
                // (20,44): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     override sealed int this[int x] {get;} = 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(20, 44),
                // (4,31): error CS0500: 'I1.this[int].get' cannot declare a body because it is marked abstract
                //     abstract int this[int x] {get => 0; set => throw null;} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.this[int].get").WithLocation(4, 31),
                // (4,41): error CS0500: 'I1.this[int].set' cannot declare a body because it is marked abstract
                //     abstract int this[int x] {get => 0; set => throw null;} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I1.this[int].set").WithLocation(4, 41),
                // (8,26): error CS0621: 'I2.this[int]': virtual or abstract members cannot be private
                //     abstract private int this[int x] => 0; 
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "this").WithArguments("I2.this[int]").WithLocation(8, 26),
                // (8,41): error CS0500: 'I2.this[int].get' cannot declare a body because it is marked abstract
                //     abstract private int this[int x] => 0; 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "0").WithArguments("I2.this[int].get").WithLocation(8, 41),
                // (12,23): error CS0106: The modifier 'static' is not valid for this item
                //     static extern int this[int x] {get; set;} 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(12, 23),
                // (16,25): error CS0106: The modifier 'static' is not valid for this item
                //     abstract static int this[int x] { get {throw null;} set {throw null;}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(16, 25),
                // (16,39): error CS0500: 'I4.this[int].get' cannot declare a body because it is marked abstract
                //     abstract static int this[int x] { get {throw null;} set {throw null;}}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I4.this[int].get").WithLocation(16, 39),
                // (16,57): error CS0500: 'I4.this[int].set' cannot declare a body because it is marked abstract
                //     abstract static int this[int x] { get {throw null;} set {throw null;}}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("I4.this[int].set").WithLocation(16, 57),
                // (20,25): error CS0106: The modifier 'override' is not valid for this item
                //     override sealed int this[int x] {get;} = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("override").WithLocation(20, 25),
                // (20,38): error CS0501: 'I5.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                //     override sealed int this[int x] {get;} = 0;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I5.this[int].get").WithLocation(20, 38),
                // (23,15): error CS0535: 'Test1' does not implement interface member 'I1.this[int]'
                // class Test1 : I1, I2, I3, I4, I5
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.this[int]"),
                // (23,19): error CS0535: 'Test1' does not implement interface member 'I2.this[int]'
                // class Test1 : I1, I2, I3, I4, I5
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Test1", "I2.this[int]"),
                // (23,27): error CS0535: 'Test1' does not implement interface member 'I4.this[int]'
                // class Test1 : I1, I2, I3, I4, I5
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I4").WithArguments("Test1", "I4.this[int]").WithLocation(23, 27),
                // (33,12): error CS0539: 'Test2.this[int]' in explicit interface declaration is not found among members of the interface that can be implemented
                //     int I5.this[int x] => 0;
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "this").WithArguments("Test2.this[int]").WithLocation(33, 12),
                // (12,36): warning CS0626: Method, operator, or accessor 'I3.this[int].get' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     static extern int this[int x] {get; set;} 
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "get").WithArguments("I3.this[int].get").WithLocation(12, 36),
                // (12,41): warning CS0626: Method, operator, or accessor 'I3.this[int].set' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     static extern int this[int x] {get; set;} 
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "set").WithArguments("I3.this[int].set").WithLocation(12, 41)
                );
        }

        [Fact]
        public void IndexerModifiers_19()
        {
            var source1 =
@"

public interface I2 {}

public interface I01{ public int I2.this[int x] {get; set;} }
public interface I02{ protected int I2.this[int x] {get;} }
public interface I03{ protected internal int I2.this[int x] {set;} }
public interface I04{ internal int I2.this[int x] {get;} }
public interface I05{ private int I2.this[int x] {set;} }
public interface I06{ static int I2.this[int x] {get;} }
public interface I07{ virtual int I2.this[int x] {set;} }
public interface I08{ sealed int I2.this[int x] {get;} }
public interface I09{ override int I2.this[int x] {set;} }
public interface I10{ abstract int I2.this[int x] {get;} }
public interface I11{ extern int I2.this[int x] {get; set;} }

public interface I12{ int I2.this[int x] { public get; set;} }
public interface I13{ int I2.this[int x] { get; protected set;} }
public interface I14{ int I2.this[int x] { protected internal get; set;} }
public interface I15{ int I2.this[int x] { get; internal set;} }
public interface I16{ int I2.this[int x] { private get; set;} }
public interface I17{ int I2.this[int x] { private get;} }
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            var expected = new[]
            {
                // (5,37): error CS0106: The modifier 'public' is not valid for this item
                // public interface I01{ public int I2.this[int x] {get; set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("public").WithLocation(5, 37),
                // (5,37): error CS0541: 'I01.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I01{ public int I2.this[int x] {get; set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I01.this[int]").WithLocation(5, 37),
                // (6,40): error CS0106: The modifier 'protected' is not valid for this item
                // public interface I02{ protected int I2.this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("protected").WithLocation(6, 40),
                // (6,40): error CS0541: 'I02.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I02{ protected int I2.this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I02.this[int]").WithLocation(6, 40),
                // (7,49): error CS0106: The modifier 'protected internal' is not valid for this item
                // public interface I03{ protected internal int I2.this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("protected internal").WithLocation(7, 49),
                // (7,49): error CS0541: 'I03.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I03{ protected internal int I2.this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I03.this[int]").WithLocation(7, 49),
                // (8,39): error CS0106: The modifier 'internal' is not valid for this item
                // public interface I04{ internal int I2.this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("internal").WithLocation(8, 39),
                // (8,39): error CS0541: 'I04.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I04{ internal int I2.this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I04.this[int]").WithLocation(8, 39),
                // (9,38): error CS0106: The modifier 'private' is not valid for this item
                // public interface I05{ private int I2.this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("private").WithLocation(9, 38),
                // (9,38): error CS0541: 'I05.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I05{ private int I2.this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I05.this[int]").WithLocation(9, 38),
                // (10,37): error CS0106: The modifier 'static' is not valid for this item
                // public interface I06{ static int I2.this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(10, 37),
                // (10,37): error CS0541: 'I06.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I06{ static int I2.this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I06.this[int]").WithLocation(10, 37),
                // (11,38): error CS0106: The modifier 'virtual' is not valid for this item
                // public interface I07{ virtual int I2.this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("virtual").WithLocation(11, 38),
                // (11,38): error CS0541: 'I07.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I07{ virtual int I2.this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I07.this[int]").WithLocation(11, 38),
                // (12,37): error CS0106: The modifier 'sealed' is not valid for this item
                // public interface I08{ sealed int I2.this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("sealed").WithLocation(12, 37),
                // (12,37): error CS0541: 'I08.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I08{ sealed int I2.this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I08.this[int]").WithLocation(12, 37),
                // (13,39): error CS0106: The modifier 'override' is not valid for this item
                // public interface I09{ override int I2.this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("override").WithLocation(13, 39),
                // (13,39): error CS0541: 'I09.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I09{ override int I2.this[int x] {set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I09.this[int]").WithLocation(13, 39),
                // (14,39): error CS0106: The modifier 'abstract' is not valid for this item
                // public interface I10{ abstract int I2.this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("abstract").WithLocation(14, 39),
                // (14,39): error CS0541: 'I10.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I10{ abstract int I2.this[int x] {get;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I10.this[int]").WithLocation(14, 39),
                // (15,37): error CS0106: The modifier 'extern' is not valid for this item
                // public interface I11{ extern int I2.this[int x] {get; set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("extern").WithLocation(15, 37),
                // (15,37): error CS0541: 'I11.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I11{ extern int I2.this[int x] {get; set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I11.this[int]").WithLocation(15, 37),
                // (17,30): error CS0541: 'I12.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I12{ int I2.this[int x] { public get; set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I12.this[int]").WithLocation(17, 30),
                // (17,51): error CS0106: The modifier 'public' is not valid for this item
                // public interface I12{ int I2.this[int x] { public get; set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("public").WithLocation(17, 51),
                // (18,30): error CS0541: 'I13.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I13{ int I2.this[int x] { get; protected set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I13.this[int]").WithLocation(18, 30),
                // (18,59): error CS0106: The modifier 'protected' is not valid for this item
                // public interface I13{ int I2.this[int x] { get; protected set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("protected").WithLocation(18, 59),
                // (19,30): error CS0541: 'I14.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I14{ int I2.this[int x] { protected internal get; set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I14.this[int]").WithLocation(19, 30),
                // (19,63): error CS0106: The modifier 'protected internal' is not valid for this item
                // public interface I14{ int I2.this[int x] { protected internal get; set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("protected internal").WithLocation(19, 63),
                // (20,30): error CS0541: 'I15.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I15{ int I2.this[int x] { get; internal set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I15.this[int]").WithLocation(20, 30),
                // (20,58): error CS0106: The modifier 'internal' is not valid for this item
                // public interface I15{ int I2.this[int x] { get; internal set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("internal").WithLocation(20, 58),
                // (21,30): error CS0541: 'I16.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I16{ int I2.this[int x] { private get; set;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I16.this[int]").WithLocation(21, 30),
                // (21,52): error CS0106: The modifier 'private' is not valid for this item
                // public interface I16{ int I2.this[int x] { private get; set;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("private").WithLocation(21, 52),
                // (22,30): error CS0541: 'I17.this[int]': explicit interface declaration can only be declared in a class or struct
                // public interface I17{ int I2.this[int x] { private get;} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "this").WithArguments("I17.this[int]").WithLocation(22, 30),
                // (22,52): error CS0106: The modifier 'private' is not valid for this item
                // public interface I17{ int I2.this[int x] { private get;} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("private").WithLocation(22, 52)
            };

            compilation1.VerifyDiagnostics(expected);

            ValidateSymbolsIndexerModifiers_19(compilation1);

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(expected);

            ValidateSymbolsIndexerModifiers_19(compilation2);
        }

        private static void ValidateSymbolsIndexerModifiers_19(CSharpCompilation compilation1)
        {

            for (int i = 1; i <= 17; i++)
            {
                var i1 = compilation1.GetTypeByMetadataName("I" + (i < 10 ? "0" : "") + i);
                ValidateSymbolsPropertyModifiers_19(i1.GetMember<PropertySymbol>("I2.this[]"));
            }
        }

        [Fact]
        public void IndexerModifiers_20()
        {
            var source1 =
@"
public interface I1
{
    internal int this[int x]
    {
        get 
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set 
        {
            System.Console.WriteLine(""set_P1"");
        }
    }

    void M2() {this[0] = this[0];}
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.M2();
    }
}
";

            ValidatePropertyModifiers_20(source1, source2);
        }

        [Fact]
        public void IndexerModifiers_21()
        {
            var source1 =
@"
public interface I1
{
    private int this[int x] { get => throw null; set => throw null; }
}
public interface I2
{
    internal int this[int x] { get => throw null; set => throw null; }
}
public interface I3
{
    public int this[int x] { get => throw null; set => throw null; }
}
public interface I4
{
    int this[int x] { get => throw null; set => throw null; }
}

class Test1
{
    static void Test(I1 i1, I2 i2, I3 i3, I4 i4)
    {
        int x;
        x = i1[0];
        i1[0] = x;
        x = i2[0];
        i2[0] = x;
        x = i3[0];
        i3[0] = x;
        x = i4[0];
        i4[0] = x;
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (24,13): error CS0122: 'I1.this[int]' is inaccessible due to its protection level
                //         x = i1[0];
                Diagnostic(ErrorCode.ERR_BadAccess, "i1[0]").WithArguments("I1.this[int]").WithLocation(24, 13),
                // (25,9): error CS0122: 'I1.this[int]' is inaccessible due to its protection level
                //         i1[0] = x;
                Diagnostic(ErrorCode.ERR_BadAccess, "i1[0]").WithArguments("I1.this[int]").WithLocation(25, 9)
                );

            var source2 =
@"
class Test2
{
    static void Test(I1 i1, I2 i2, I3 i3, I4 i4)
    {
        int x;
        x = i1[0];
        i1[0] = x;
        x = i2[0];
        i2[0] = x;
        x = i3[0];
        i3[0] = x;
        x = i4[0];
        i4[0] = x;
    }
}
";
            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() },
                                                         options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(
                // (7,13): error CS0122: 'I1.this[int]' is inaccessible due to its protection level
                //         x = i1[0];
                Diagnostic(ErrorCode.ERR_BadAccess, "i1[0]").WithArguments("I1.this[int]").WithLocation(7, 13),
                // (8,9): error CS0122: 'I1.this[int]' is inaccessible due to its protection level
                //         i1[0] = x;
                Diagnostic(ErrorCode.ERR_BadAccess, "i1[0]").WithArguments("I1.this[int]").WithLocation(8, 9),
                // (9,13): error CS0122: 'I2.this[int]' is inaccessible due to its protection level
                //         x = i2[0];
                Diagnostic(ErrorCode.ERR_BadAccess, "i2[0]").WithArguments("I2.this[int]").WithLocation(9, 13),
                // (10,9): error CS0122: 'I2.this[int]' is inaccessible due to its protection level
                //         i2[0] = x;
                Diagnostic(ErrorCode.ERR_BadAccess, "i2[0]").WithArguments("I2.this[int]").WithLocation(10, 9)
                );
        }

        [Fact]
        public void IndexerModifiers_22()
        {
            var source1 =
@"
public interface I1
{
    public int this[int x] 
    {
        internal get
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set 
        {
            System.Console.WriteLine(""set_P1"");
        }
    }
}
public interface I2
{
    int this[int x] 
    {
        get
        {
            System.Console.WriteLine(""get_P2"");
            return 0;
        }
        internal set
        {
            System.Console.WriteLine(""set_P2"");
        }
    }
}
public interface I3
{
    int this[int x] 
    {
        internal get => Test1.GetP3();
        set => System.Console.WriteLine(""set_P3"");
    }
}
public interface I4
{
    int this[int x]
    {
        get => Test1.GetP4();
        internal set => System.Console.WriteLine(""set_P4"");
    }
}
public interface I5
{
    int this[int x] 
    {
        private get
        {
            System.Console.WriteLine(""get_P5"");
            return 0;
        }
        set 
        {
            System.Console.WriteLine(""set_P5"");
        }
    }

    void Test()
    {
        this[0] = this[0];
    }
}
public interface I6
{
    int this[int x] 
    {
        get
        {
            System.Console.WriteLine(""get_P6"");
            return 0;
        }
        private set
        {
            System.Console.WriteLine(""set_P6"");
        }
    }

    void Test()
    {
        this[0] = this[0];
    }
}
public interface I7
{
    int this[int x] 
    {
        private get => Test1.GetP7();
        set => System.Console.WriteLine(""set_P7"");
    }

    void Test()
    {
        this[0] = this[0];
    }
}
public interface I8
{
    int this[int x]
    {
        get => Test1.GetP8();
        private set => System.Console.WriteLine(""set_P8"");
    }

    void Test()
    {
        this[0] = this[0];
    }
}

class Test1 : I1, I2, I3, I4, I5, I6, I7, I8
{
    static void Main()
    {
        I1 i1 = new Test1();
        I2 i2 = new Test1();
        I3 i3 = new Test1();
        I4 i4 = new Test1();
        I5 i5 = new Test1();
        I6 i6 = new Test1();
        I7 i7 = new Test1();
        I8 i8 = new Test1();

        i1[0] = i1[0];
        i2[0] = i2[0];
        i3[0] = i3[0];
        i4[0] = i4[0];
        i5.Test();
        i6.Test();
        i7.Test();
        i8.Test();
    }

    public static int GetP3()
    {
        System.Console.WriteLine(""get_P3"");
        return 0;
    }

    public static int GetP4()
    {
        System.Console.WriteLine(""get_P4"");
        return 0;
    }

    public static int GetP7()
    {
        System.Console.WriteLine(""get_P7"");
        return 0;
    }

    public static int GetP8()
    {
        System.Console.WriteLine(""get_P8"");
        return 0;
    }
}
";

            ValidatePropertyModifiers_22(source1);
        }

        [Fact]
        public void IndexerModifiers_23()
        {
            var source1 =
@"
public interface I1
{
    abstract int this[int x] {internal get; set;} 

    void M2()
    {
        this[0] = this[1];
    }
}
public interface I3
{
    int this[int x]
    {
        private get 
        {
            System.Console.WriteLine(""get_P3"");
            return 0;
        } 
        set {}
    }

    void M2()
    {
        this[0] = this[1];
    }
}
public interface I4
{
    int this[int x]
    {
        get {throw null;} 
        private set {System.Console.WriteLine(""set_P4"");}
    }

    void M2()
    {
        this[0] = this[1];
    }
}
public interface I5
{
    int this[int x]
    {
        private get => GetP5();
        set => throw null;
    }

    private int GetP5()
    {
        System.Console.WriteLine(""get_P5"");
        return 0;
    }

    void M2()
    {
        this[0] = this[1];
    }
}
public interface I6
{
    int this[int x]
    {
        get => throw null;
        private set => System.Console.WriteLine(""set_P6"");
    }

    void M2()
    {
        this[0] = this[1];
    }
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 i1 = new Test1();
        I3 i3 = new Test3();
        I4 i4 = new Test4();
        I5 i5 = new Test5();
        I6 i6 = new Test6();
        i1.M2();
        i3.M2();
        i4.M2();
        i5.M2();
        i6.M2();
    }

    public int this[int x] 
    {
        get
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        set
        {
            System.Console.WriteLine(""set_P1"");
        }
    }
}
class Test3 : I3
{
    public int this[int x] 
    {
        get
        {
            throw null;
        }
        set
        {
            System.Console.WriteLine(""set_P3"");
        }
    }
}
class Test4 : I4
{
    public int this[int x] 
    {
        get
        {
            System.Console.WriteLine(""get_P4"");
            return 0;
        }
        set
        {
            throw null;
        }
    }
}
class Test5 : I5
{
    public int this[int x] 
    {
        get
        {
            throw null;
        }
        set
        {
            System.Console.WriteLine(""set_P5"");
        }
    }
}
class Test6 : I6
{
    public int this[int x] 
    {
        get
        {
            System.Console.WriteLine(""get_P6"");
            return 0;
        }
        set
        {
            throw null;
        }
    }
}
";
            ValidatePropertyModifiers_23(source1, source2);
        }

        [Fact]
        public void IndexerModifiers_24()
        {
            var source1 =
@"
public interface I1
{
    int this[int x]
    {
        get 
        {
            System.Console.WriteLine(""get_P1"");
            return 0;
        }
        internal set 
        {
            System.Console.WriteLine(""set_P1"");
        }
    }

    void M2() {this[0] = this[1];}
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.M2();
    }
}
";
            ValidatePropertyModifiers_24(source1, source2);
        }

        [Fact]
        public void IndexerModifiers_25()
        {
            var source1 =
@"
public interface I1
{
    int this[int x] { private get => throw null; set => throw null; }
}
public interface I2
{
    int this[int x] { internal get => throw null; set => throw null; }
}
public interface I3
{
    public int this[int x] { get => throw null; private set => throw null; }
}
public interface I4
{
    int this[int x] { get => throw null; internal set => throw null; }
}

public class Test1 : I1, I2, I3, I4
{
    static void Main()
    {
        int x;
        I1 i1 = new Test1();
        I2 i2 = new Test1();
        I3 i3 = new Test1();
        I4 i4 = new Test1();

        x = i1[0];
        i1[0] = x;
        x = i2[0];
        i2[0] = x;
        x = i3[0];
        i3[0] = x;
        x = i4[0];
        i4[0] = x;
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (29,13): error CS0271: The property or indexer 'I1.this[int]' cannot be used in this context because the get accessor is inaccessible
                //         x = i1[0];
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "i1[0]").WithArguments("I1.this[int]").WithLocation(29, 13),
                // (34,9): error CS0272: The property or indexer 'I3.this[int]' cannot be used in this context because the set accessor is inaccessible
                //         i3[0] = x;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "i3[0]").WithArguments("I3.this[int]").WithLocation(34, 9)
                );

            var source2 =
@"
class Test2
{
    static void Main()
    {
        int x;
        I1 i1 = new Test1();
        I2 i2 = new Test1();
        I3 i3 = new Test1();
        I4 i4 = new Test1();

        x = i1[0];
        i1[0] = x;
        x = i2[0];
        i2[0] = x;
        x = i3[0];
        i3[0] = x;
        x = i4[0];
        i4[0] = x;
    }
}
";
            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() },
                                                         options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(
                // (12,13): error CS0271: The property or indexer 'I1.this[int]' cannot be used in this context because the get accessor is inaccessible
                //         x = i1[0];
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "i1[0]").WithArguments("I1.this[int]").WithLocation(12, 13),
                // (14,13): error CS0271: The property or indexer 'I2.this[int]' cannot be used in this context because the get accessor is inaccessible
                //         x = i2[0];
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "i2[0]").WithArguments("I2.this[int]").WithLocation(14, 13),
                // (17,9): error CS0272: The property or indexer 'I3.this[int]' cannot be used in this context because the set accessor is inaccessible
                //         i3[0] = x;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "i3[0]").WithArguments("I3.this[int]").WithLocation(17, 9),
                // (19,9): error CS0272: The property or indexer 'I4.this[int]' cannot be used in this context because the set accessor is inaccessible
                //         i4[0] = x;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "i4[0]").WithArguments("I4.this[int]").WithLocation(19, 9)
                );
        }

        [Fact]
        public void IndexerModifiers_26()
        {
            var source1 =
@"
public interface I1
{
    abstract int this[sbyte x] { private get; set; }
    abstract int this[byte x] { get; private set; }
    abstract int this[short x] { internal get; }
    int this[ushort x] {internal get;} = 0;
    int this[int x] { internal get {throw null;} }
    int this[uint x] { internal set {throw null;} }
    int this[long x] { internal get => throw null; }
    int this[ulong x] { internal set => throw null; }
    int this[float x] { internal get {throw null;} private set {throw null;}}
    int this[double x] { internal get => throw null; private set => throw null;}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (7,40): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                //     int this[ushort x] {internal get;} = 0;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(7, 40),
                // (4,42): error CS0442: 'I1.this[sbyte].get': abstract properties cannot have private accessors
                //     abstract int this[sbyte x] { private get; set; }
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "get").WithArguments("I1.this[sbyte].get").WithLocation(4, 42),
                // (5,46): error CS0442: 'I1.this[byte].set': abstract properties cannot have private accessors
                //     abstract int this[byte x] { get; private set; }
                Diagnostic(ErrorCode.ERR_PrivateAbstractAccessor, "set").WithArguments("I1.this[byte].set").WithLocation(5, 46),
                // (6,18): error CS0276: 'I1.this[short]': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     abstract int this[short x] { internal get; }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "this").WithArguments("I1.this[short]").WithLocation(6, 18),
                // (7,9): error CS0276: 'I1.this[ushort]': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     int this[ushort x] {internal get;} = 0;
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "this").WithArguments("I1.this[ushort]").WithLocation(7, 9),
                // (8,9): error CS0276: 'I1.this[int]': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     int this[int x] { internal get {throw null;} }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "this").WithArguments("I1.this[int]").WithLocation(8, 9),
                // (9,9): error CS0276: 'I1.this[uint]': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     int this[uint x] { internal set {throw null;} }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "this").WithArguments("I1.this[uint]").WithLocation(9, 9),
                // (10,9): error CS0276: 'I1.this[long]': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     int this[long x] { internal get => throw null; }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "this").WithArguments("I1.this[long]").WithLocation(10, 9),
                // (11,9): error CS0276: 'I1.this[ulong]': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor
                //     int this[ulong x] { internal set => throw null; }
                Diagnostic(ErrorCode.ERR_AccessModMissingAccessor, "this").WithArguments("I1.this[ulong]").WithLocation(11, 9),
                // (12,9): error CS0274: Cannot specify accessibility modifiers for both accessors of the property or indexer 'I1.this[float]'
                //     int this[float x] { internal get {throw null;} private set {throw null;}}
                Diagnostic(ErrorCode.ERR_DuplicatePropertyAccessMods, "this").WithArguments("I1.this[float]").WithLocation(12, 9),
                // (13,9): error CS0274: Cannot specify accessibility modifiers for both accessors of the property or indexer 'I1.this[double]'
                //     int this[double x] { internal get => throw null; private set => throw null;}
                Diagnostic(ErrorCode.ERR_DuplicatePropertyAccessMods, "this").WithArguments("I1.this[double]").WithLocation(13, 9)
                );
        }

        [Fact]
        public void IndexerModifiers_27()
        {
            var source1 =
@"
public interface I1
{
    int this[short x]
    {
        private get {throw null;} 
        set {}
    }

    int this[int x]
    {
        get {throw null;} 
        private set {}
    }
}

class Test1 : I1
{
    int I1.this[short x]
    {
        get {throw null;} 
        set {}
    }

    int I1.this[int x]
    {
        get {throw null;} 
        set {}
    }
}

class Test2 : I1
{
    int I1.this[short x]
    {
        set {throw null;}
    }

    int I1.this[int x]
    {
        get => throw null; 
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            // PROTOTYPE(DefaultInterfaceImplementation): The lack of errors for Test1 looks wrong. Private accessor is never virtual 
            //                                            (the behavior goes back to native compiler). So, it would be wrong
            //                                            to have a MethodImpl to point to an accessor like this in an interface. 
            compilation1.VerifyDiagnostics(
                // (34,12): error CS0551: Explicit interface implementation 'Test2.I1.this[short]' is missing accessor 'I1.this[short].get'
                //     int I1.this[short x]
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "this").WithArguments("Test2.I1.this[short]", "I1.this[short].get").WithLocation(34, 12),
                // (39,12): error CS0551: Explicit interface implementation 'Test2.I1.this[int]' is missing accessor 'I1.this[int].set'
                //     int I1.this[int x]
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "this").WithArguments("Test2.I1.this[int]", "I1.this[int].set").WithLocation(39, 12)
                );
        }

        [Fact]
        public void EventModifiers_01()
        {
            var source1 =
@"
public interface I1
{
    public event System.Action P01;
    protected event System.Action P02 {add{}}
    protected internal event System.Action P03 {remove{}}
    internal event System.Action P04 {add{}}
    private event System.Action P05 {remove{}}
    static event System.Action P06 {add{}}
    virtual event System.Action P07 {remove{}}
    sealed event System.Action P08 {add{}}
    override event System.Action P09 {remove{}}
    abstract event System.Action P10 {add{}}
    extern event System.Action P11 {add{} remove{}}
    extern event System.Action P12 {add; remove;}
    extern event System.Action P13;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.ERR_EventNeedsBothAccessors).Verify(
                // (5,35): error CS0106: The modifier 'protected' is not valid for this item
                //     protected event System.Action P02 {add{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P02").WithArguments("protected").WithLocation(5, 35),
                // (6,44): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal event System.Action P03 {remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P03").WithArguments("protected internal").WithLocation(6, 44),
                // (12,34): error CS0106: The modifier 'override' is not valid for this item
                //     override event System.Action P09 {remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P09").WithArguments("override").WithLocation(12, 34),
                // (13,39): error CS0500: 'I1.P10.add' cannot declare a body because it is marked abstract
                //     abstract event System.Action P10 {add{}}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "add").WithArguments("I1.P10.add").WithLocation(13, 39),
                // (14,37): error CS0179: 'I1.P11.add' cannot be extern and declare a body
                //     extern event System.Action P11 {add{} remove{}}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "add").WithArguments("I1.P11.add").WithLocation(14, 37),
                // (14,43): error CS0179: 'I1.P11.remove' cannot be extern and declare a body
                //     extern event System.Action P11 {add{} remove{}}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "remove").WithArguments("I1.P11.remove").WithLocation(14, 43),
                // (15,37): warning CS0626: Method, operator, or accessor 'I1.P12.add' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern event System.Action P12 {add; remove;}
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "add").WithArguments("I1.P12.add").WithLocation(15, 37),
                // (15,40): error CS0073: An add or remove accessor must have a body
                //     extern event System.Action P12 {add; remove;}
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(15, 40),
                // (15,42): warning CS0626: Method, operator, or accessor 'I1.P12.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern event System.Action P12 {add; remove;}
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "remove").WithArguments("I1.P12.remove").WithLocation(15, 42),
                // (15,48): error CS0073: An add or remove accessor must have a body
                //     extern event System.Action P12 {add; remove;}
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(15, 48),
                // (16,32): warning CS0626: Method, operator, or accessor 'I1.P13.add' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern event System.Action P13;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "P13").WithArguments("I1.P13.add").WithLocation(16, 32),
                // (16,32): warning CS0626: Method, operator, or accessor 'I1.P13.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern event System.Action P13;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "P13").WithArguments("I1.P13.remove").WithLocation(16, 32)
                );

            ValidateSymbolsEventModifiers_01(compilation1);
        }

        private static void ValidateSymbolsEventModifiers_01(CSharpCompilation compilation1)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p01 = i1.GetMember<EventSymbol>("P01");

            Assert.True(p01.IsAbstract);
            Assert.False(p01.IsVirtual);
            Assert.False(p01.IsSealed);
            Assert.False(p01.IsStatic);
            Assert.False(p01.IsExtern);
            Assert.False(p01.IsOverride);
            Assert.Equal(Accessibility.Public, p01.DeclaredAccessibility);

            VaidateP01Accessor(p01.AddMethod);
            VaidateP01Accessor(p01.RemoveMethod);
            void VaidateP01Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }

            var p02 = i1.GetMember<EventSymbol>("P02");
            var p02get = p02.AddMethod;

            Assert.False(p02.IsAbstract);
            Assert.True(p02.IsVirtual);
            Assert.False(p02.IsSealed);
            Assert.False(p02.IsStatic);
            Assert.False(p02.IsExtern);
            Assert.False(p02.IsOverride);
            Assert.Equal(Accessibility.Public, p02.DeclaredAccessibility);

            Assert.False(p02get.IsAbstract);
            Assert.True(p02get.IsVirtual);
            Assert.True(p02get.IsMetadataVirtual());
            Assert.False(p02get.IsSealed);
            Assert.False(p02get.IsStatic);
            Assert.False(p02get.IsExtern);
            Assert.False(p02get.IsAsync);
            Assert.False(p02get.IsOverride);
            Assert.Equal(Accessibility.Public, p02get.DeclaredAccessibility);

            var p03 = i1.GetMember<EventSymbol>("P03");
            var p03set = p03.RemoveMethod;

            Assert.False(p03.IsAbstract);
            Assert.True(p03.IsVirtual);
            Assert.False(p03.IsSealed);
            Assert.False(p03.IsStatic);
            Assert.False(p03.IsExtern);
            Assert.False(p03.IsOverride);
            Assert.Equal(Accessibility.Public, p03.DeclaredAccessibility);

            Assert.False(p03set.IsAbstract);
            Assert.True(p03set.IsVirtual);
            Assert.True(p03set.IsMetadataVirtual());
            Assert.False(p03set.IsSealed);
            Assert.False(p03set.IsStatic);
            Assert.False(p03set.IsExtern);
            Assert.False(p03set.IsAsync);
            Assert.False(p03set.IsOverride);
            Assert.Equal(Accessibility.Public, p03set.DeclaredAccessibility);

            var p04 = i1.GetMember<EventSymbol>("P04");
            var p04get = p04.AddMethod;

            Assert.False(p04.IsAbstract);
            Assert.True(p04.IsVirtual);
            Assert.False(p04.IsSealed);
            Assert.False(p04.IsStatic);
            Assert.False(p04.IsExtern);
            Assert.False(p04.IsOverride);
            Assert.Equal(Accessibility.Internal, p04.DeclaredAccessibility);

            Assert.False(p04get.IsAbstract);
            Assert.True(p04get.IsVirtual);
            Assert.True(p04get.IsMetadataVirtual());
            Assert.False(p04get.IsSealed);
            Assert.False(p04get.IsStatic);
            Assert.False(p04get.IsExtern);
            Assert.False(p04get.IsAsync);
            Assert.False(p04get.IsOverride);
            Assert.Equal(Accessibility.Internal, p04get.DeclaredAccessibility);

            var p05 = i1.GetMember<EventSymbol>("P05");
            var p05set = p05.RemoveMethod;

            Assert.False(p05.IsAbstract);
            Assert.False(p05.IsVirtual);
            Assert.False(p05.IsSealed);
            Assert.False(p05.IsStatic);
            Assert.False(p05.IsExtern);
            Assert.False(p05.IsOverride);
            Assert.Equal(Accessibility.Private, p05.DeclaredAccessibility);

            Assert.False(p05set.IsAbstract);
            Assert.False(p05set.IsVirtual);
            Assert.False(p05set.IsMetadataVirtual());
            Assert.False(p05set.IsSealed);
            Assert.False(p05set.IsStatic);
            Assert.False(p05set.IsExtern);
            Assert.False(p05set.IsAsync);
            Assert.False(p05set.IsOverride);
            Assert.Equal(Accessibility.Private, p05set.DeclaredAccessibility);

            var p06 = i1.GetMember<EventSymbol>("P06");
            var p06get = p06.AddMethod;

            Assert.False(p06.IsAbstract);
            Assert.False(p06.IsVirtual);
            Assert.False(p06.IsSealed);
            Assert.True(p06.IsStatic);
            Assert.False(p06.IsExtern);
            Assert.False(p06.IsOverride);
            Assert.Equal(Accessibility.Public, p06.DeclaredAccessibility);

            Assert.False(p06get.IsAbstract);
            Assert.False(p06get.IsVirtual);
            Assert.False(p06get.IsMetadataVirtual());
            Assert.False(p06get.IsSealed);
            Assert.True(p06get.IsStatic);
            Assert.False(p06get.IsExtern);
            Assert.False(p06get.IsAsync);
            Assert.False(p06get.IsOverride);
            Assert.Equal(Accessibility.Public, p06get.DeclaredAccessibility);

            var p07 = i1.GetMember<EventSymbol>("P07");
            var p07set = p07.RemoveMethod;

            Assert.False(p07.IsAbstract);
            Assert.True(p07.IsVirtual);
            Assert.False(p07.IsSealed);
            Assert.False(p07.IsStatic);
            Assert.False(p07.IsExtern);
            Assert.False(p07.IsOverride);
            Assert.Equal(Accessibility.Public, p07.DeclaredAccessibility);

            Assert.False(p07set.IsAbstract);
            Assert.True(p07set.IsVirtual);
            Assert.True(p07set.IsMetadataVirtual());
            Assert.False(p07set.IsSealed);
            Assert.False(p07set.IsStatic);
            Assert.False(p07set.IsExtern);
            Assert.False(p07set.IsAsync);
            Assert.False(p07set.IsOverride);
            Assert.Equal(Accessibility.Public, p07set.DeclaredAccessibility);

            var p08 = i1.GetMember<EventSymbol>("P08");
            var p08get = p08.AddMethod;

            Assert.False(p08.IsAbstract);
            Assert.False(p08.IsVirtual);
            Assert.False(p08.IsSealed);
            Assert.False(p08.IsStatic);
            Assert.False(p08.IsExtern);
            Assert.False(p08.IsOverride);
            Assert.Equal(Accessibility.Public, p08.DeclaredAccessibility);

            Assert.False(p08get.IsAbstract);
            Assert.False(p08get.IsVirtual);
            Assert.False(p08get.IsMetadataVirtual());
            Assert.False(p08get.IsSealed);
            Assert.False(p08get.IsStatic);
            Assert.False(p08get.IsExtern);
            Assert.False(p08get.IsAsync);
            Assert.False(p08get.IsOverride);
            Assert.Equal(Accessibility.Public, p08get.DeclaredAccessibility);

            var p09 = i1.GetMember<EventSymbol>("P09");
            var p09set = p09.RemoveMethod;

            Assert.False(p09.IsAbstract);
            Assert.True(p09.IsVirtual);
            Assert.False(p09.IsSealed);
            Assert.False(p09.IsStatic);
            Assert.False(p09.IsExtern);
            Assert.False(p09.IsOverride);
            Assert.Equal(Accessibility.Public, p09.DeclaredAccessibility);

            Assert.False(p09set.IsAbstract);
            Assert.True(p09set.IsVirtual);
            Assert.True(p09set.IsMetadataVirtual());
            Assert.False(p09set.IsSealed);
            Assert.False(p09set.IsStatic);
            Assert.False(p09set.IsExtern);
            Assert.False(p09set.IsAsync);
            Assert.False(p09set.IsOverride);
            Assert.Equal(Accessibility.Public, p09set.DeclaredAccessibility);

            var p10 = i1.GetMember<EventSymbol>("P10");
            var p10get = p10.AddMethod;

            Assert.True(p10.IsAbstract);
            Assert.False(p10.IsVirtual);
            Assert.False(p10.IsSealed);
            Assert.False(p10.IsStatic);
            Assert.False(p10.IsExtern);
            Assert.False(p10.IsOverride);
            Assert.Equal(Accessibility.Public, p10.DeclaredAccessibility);

            Assert.True(p10get.IsAbstract);
            Assert.False(p10get.IsVirtual);
            Assert.True(p10get.IsMetadataVirtual());
            Assert.False(p10get.IsSealed);
            Assert.False(p10get.IsStatic);
            Assert.False(p10get.IsExtern);
            Assert.False(p10get.IsAsync);
            Assert.False(p10get.IsOverride);
            Assert.Equal(Accessibility.Public, p10get.DeclaredAccessibility);

            foreach (var name in new[] { "P11", "P12", "P13" })
            {
                var p11 = i1.GetMember<EventSymbol>(name);

                Assert.False(p11.IsAbstract);
                Assert.True(p11.IsVirtual);
                Assert.False(p11.IsSealed);
                Assert.False(p11.IsStatic);
                Assert.True(p11.IsExtern);
                Assert.False(p11.IsOverride);
                Assert.Equal(Accessibility.Public, p11.DeclaredAccessibility);

                ValidateP11Accessor(p11.AddMethod);
                ValidateP11Accessor(p11.RemoveMethod);
                void ValidateP11Accessor(MethodSymbol accessor)
                {
                    Assert.False(accessor.IsAbstract);
                    Assert.True(accessor.IsVirtual);
                    Assert.True(accessor.IsMetadataVirtual());
                    Assert.False(accessor.IsSealed);
                    Assert.False(accessor.IsStatic);
                    Assert.True(accessor.IsExtern);
                    Assert.False(accessor.IsAsync);
                    Assert.False(accessor.IsOverride);
                    Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                }
            }
        }

        [Fact]
        public void EventModifiers_02()
        {
            var source1 =
@"
public interface I1
{
    public event System.Action P01;
    protected event System.Action P02 {add{}}
    protected internal event System.Action P03 {remove{}}
    internal event System.Action P04 {add{}}
    private event System.Action P05 {remove{}}
    static event System.Action P06 {add{}}
    virtual event System.Action P07 {remove{}}
    sealed event System.Action P08 {add{}}
    override event System.Action P09 {remove{}}
    abstract event System.Action P10 {add{}}
    extern event System.Action P11 {add{} remove{}}
    extern event System.Action P12 {add; remove;}
    extern event System.Action P13;
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.GetDiagnostics().Where(d => d.Code != (int)ErrorCode.ERR_EventNeedsBothAccessors).Verify(
                // (4,32): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public event System.Action P01;
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P01").WithArguments("public", "7", "7.1").WithLocation(4, 32),
                // (5,35): error CS0106: The modifier 'protected' is not valid for this item
                //     protected event System.Action P02 {add{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P02").WithArguments("protected").WithLocation(5, 35),
                // (5,40): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     protected event System.Action P02 {add{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(5, 40),
                // (6,44): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal event System.Action P03 {remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P03").WithArguments("protected internal").WithLocation(6, 44),
                // (6,49): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     protected internal event System.Action P03 {remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(6, 49),
                // (7,39): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     internal event System.Action P04 {add{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(7, 39),
                // (8,38): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     private event System.Action P05 {remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(8, 38),
                // (9,37): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     static event System.Action P06 {add{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(9, 37),
                // (10,38): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     virtual event System.Action P07 {remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(10, 38),
                // (11,37): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     sealed event System.Action P08 {add{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(11, 37),
                // (12,34): error CS0106: The modifier 'override' is not valid for this item
                //     override event System.Action P09 {remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P09").WithArguments("override").WithLocation(12, 34),
                // (12,39): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     override event System.Action P09 {remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(12, 39),
                // (13,39): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     abstract event System.Action P10 {add{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(13, 39),
                // (13,39): error CS0500: 'I1.P10.add' cannot declare a body because it is marked abstract
                //     abstract event System.Action P10 {add{}}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "add").WithArguments("I1.P10.add").WithLocation(13, 39),
                // (14,37): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     extern event System.Action P11 {add{} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(14, 37),
                // (14,37): error CS0179: 'I1.P11.add' cannot be extern and declare a body
                //     extern event System.Action P11 {add{} remove{}}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "add").WithArguments("I1.P11.add").WithLocation(14, 37),
                // (14,43): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     extern event System.Action P11 {add{} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(14, 43),
                // (14,43): error CS0179: 'I1.P11.remove' cannot be extern and declare a body
                //     extern event System.Action P11 {add{} remove{}}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "remove").WithArguments("I1.P11.remove").WithLocation(14, 43),
                // (15,37): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     extern event System.Action P12 {add; remove;}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(15, 37),
                // (15,37): warning CS0626: Method, operator, or accessor 'I1.P12.add' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern event System.Action P12 {add; remove;}
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "add").WithArguments("I1.P12.add").WithLocation(15, 37),
                // (15,40): error CS0073: An add or remove accessor must have a body
                //     extern event System.Action P12 {add; remove;}
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(15, 40),
                // (15,42): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     extern event System.Action P12 {add; remove;}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(15, 42),
                // (15,42): warning CS0626: Method, operator, or accessor 'I1.P12.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern event System.Action P12 {add; remove;}
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "remove").WithArguments("I1.P12.remove").WithLocation(15, 42),
                // (15,48): error CS0073: An add or remove accessor must have a body
                //     extern event System.Action P12 {add; remove;}
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(15, 48),
                // (16,32): error CS8503: The modifier 'extern' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     extern event System.Action P13;
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P13").WithArguments("extern", "7", "7.1").WithLocation(16, 32),
                // (16,32): warning CS0626: Method, operator, or accessor 'I1.P13.add' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern event System.Action P13;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "P13").WithArguments("I1.P13.add").WithLocation(16, 32),
                // (16,32): warning CS0626: Method, operator, or accessor 'I1.P13.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern event System.Action P13;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "P13").WithArguments("I1.P13.remove").WithLocation(16, 32)
                );

            ValidateSymbolsEventModifiers_01(compilation1);
        }

        [Fact]
        public void EventModifiers_03()
        {
            ValidateEventImplementation_102(@"
public interface I1
{
    public virtual event System.Action E1 
    {
        add => System.Console.WriteLine(""add E1"");
        remove => System.Console.WriteLine(""remove E1"");
    }
}

class Test1 : I1
{}
");
            ValidateEventImplementation_102(@"
public interface I1
{
    public virtual event System.Action E1 
    {
        add {System.Console.WriteLine(""add E1"");}
        remove {System.Console.WriteLine(""remove E1"");}
    }
}

class Test1 : I1
{}
");
        }

        [Fact]
        public void EventModifiers_04()
        {
            ValidateEventImplementation_101(@"
public interface I1
{
    public virtual event System.Action E1 {}
}

class Test1 : I1
{}
",
                new[] {
                // (4,40): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     public virtual event System.Action E1 {}
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 40)
                },
                haveAdd: false, haveRemove: false);

            ValidateEventImplementation_101(@"
public interface I1
{
    public virtual event System.Action E1 
    {
        add;
    }
}

class Test1 : I1
{}
",
                new[] {
                // (6,12): error CS0073: An add or remove accessor must have a body
                //         add;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";"),
                // (4,40): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     public virtual event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 40)
                },
                haveAdd: true, haveRemove: false);

            ValidateEventImplementation_101(@"
public interface I1
{
    public virtual event System.Action E1 
    {
        add {}
    }
}

class Test1 : I1
{}
",
                new[] {
                // (4,40): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     public virtual event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 40)
                },
                haveAdd: true, haveRemove: false);


            ValidateEventImplementation_101(@"
public interface I1
{
    public virtual event System.Action E1 
    {
        add => throw null;
    }
}

class Test1 : I1
{}
",
                new[] {
                // (4,40): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     public virtual event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 40)
                },
                haveAdd: true, haveRemove: false);

            ValidateEventImplementation_101(@"
public interface I1
{
    public virtual event System.Action E1 
    {
        remove;
    }
}

class Test1 : I1
{}
",
                new[] {
                // (6,15): error CS0073: An add or remove accessor must have a body
                //         remove;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(6, 15),
                // (4,40): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     public virtual event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 40)
                },
                haveAdd: false, haveRemove: true);

            ValidateEventImplementation_101(@"
public interface I1
{
    public virtual event System.Action E1 
    {
        remove {}
    }
}

class Test1 : I1
{}
",
                new[] {
                // (4,40): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     public virtual event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 40)
                },
                haveAdd: false, haveRemove: true);

            ValidateEventImplementation_101(@"
public interface I1
{
    public virtual event System.Action E1 
    {
        remove => throw null;
    }
}

class Test1 : I1
{}
",
                new[] {
                // (4,40): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     public virtual event System.Action E1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 40)
                },
                haveAdd: false, haveRemove: true);

            ValidateEventImplementation_101(@"
public interface I1
{
    public virtual event System.Action E1 
    {
        add;
        remove;
    }
}

class Test1 : I1
{}
",
                new[] {
                // (6,12): error CS0073: An add or remove accessor must have a body
                //         add;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";"),
                // (7,15): error CS0073: An add or remove accessor must have a body
                //         remove;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(7, 15)
                },
                haveAdd: true, haveRemove: true);

            ValidateEventImplementation_101(@"
public interface I1
{
    public virtual event System.Action E1;
}

class Test1 : I1
{}
",
                new[] {
                // (4,40): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     public virtual event System.Action E1;
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 40)
                },
                haveAdd: true, haveRemove: true);

            ValidateEventImplementation_101(@"
public interface I1
{
    public virtual event System.Action E1 = null;
}

class Test1 : I1
{}
",
                new[] {
                // (4,40): error CS0068: 'I1.E1': event in interface cannot have initializer
                //     public virtual event System.Action E1 = null;
                Diagnostic(ErrorCode.ERR_InterfaceEventInitializer, "E1").WithArguments("I1.E1").WithLocation(4, 40),
                // (4,40): error CS0065: 'I1.E1': event property must have both add and remove accessors
                //     public virtual event System.Action E1 = null;
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("I1.E1").WithLocation(4, 40),
                // (4,40): warning CS0067: The event 'I1.E1' is never used
                //     public virtual event System.Action E1 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("I1.E1").WithLocation(4, 40)
                },
                haveAdd: true, haveRemove: true);
        }

        [Fact]
        public void EventModifiers_05()
        {
            var source1 =
@"
public interface I1
{
    public abstract event System.Action P1; 
}
public interface I2
{
    event System.Action P2;
}

class Test1 : I1
{
    public event System.Action P1 
    {
        add
        {
            System.Console.WriteLine(""get_P1"");
        }
        remove => System.Console.WriteLine(""set_P1"");
    }
}
class Test2 : I2
{
    public event System.Action P2 
    {
        add
        {
            System.Console.WriteLine(""get_P2"");
        }
        remove => System.Console.WriteLine(""set_P2"");
    }

    static void Main()
    {
        I1 x = new Test1();
        x.P1 += null;
        x.P1 -= null;
        I2 y = new Test2();
        y.P2 += null;
        y.P2 -= null;
    }
}
";

            ValidateEventModifiers_05(source1);
        }

        private void ValidateEventModifiers_05(string source1)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            CompileAndVerify(compilation1, expectedOutput:
@"get_P1
set_P1
get_P2
set_P2", symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                for (int i = 1; i <= 2; i++)
                {
                    var test1 = m.GlobalNamespace.GetTypeMember("Test" + i);
                    var i1 = m.GlobalNamespace.GetTypeMember("I" + i);
                    var p1 = GetSingleEvent(i1);
                    var test1P1 = GetSingleEvent(test1);

                    Assert.True(p1.IsAbstract);
                    Assert.False(p1.IsVirtual);
                    Assert.False(p1.IsSealed);
                    Assert.False(p1.IsStatic);
                    Assert.False(p1.IsExtern);
                    Assert.False(p1.IsOverride);
                    Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
                    Assert.Same(test1P1, test1.FindImplementationForInterfaceMember(p1));

                    ValidateAccessor(p1.AddMethod, test1P1.AddMethod);
                    ValidateAccessor(p1.RemoveMethod, test1P1.RemoveMethod);

                    void ValidateAccessor(MethodSymbol accessor, MethodSymbol implementation)
                    {
                        Assert.True(accessor.IsAbstract);
                        Assert.False(accessor.IsVirtual);
                        Assert.True(accessor.IsMetadataVirtual());
                        Assert.False(accessor.IsSealed);
                        Assert.False(accessor.IsStatic);
                        Assert.False(accessor.IsExtern);
                        Assert.False(accessor.IsAsync);
                        Assert.False(accessor.IsOverride);
                        Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                        Assert.Same(implementation, test1.FindImplementationForInterfaceMember(accessor));
                    }
                }
            }
        }

        private static EventSymbol GetSingleEvent(NamedTypeSymbol container)
        {
            return container.GetMembers().OfType<EventSymbol>().Single();
        }

        private static EventSymbol GetSingleEvent(CSharpCompilation compilation, string containerName)
        {
            return GetSingleEvent(compilation.GetTypeByMetadataName(containerName));
        }

        private static EventSymbol GetSingleEvent(ModuleSymbol m, string containerName)
        {
            return GetSingleEvent(m.GlobalNamespace.GetTypeMember(containerName));
        }

        [Fact]
        public void EventModifiers_06()
        {
            var source1 =
@"
public interface I1
{
    public abstract event System.Action P1; 
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,41): error CS8503: The modifier 'abstract' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public abstract event System.Action P1; 
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P1").WithArguments("abstract", "7", "7.1").WithLocation(4, 41),
                // (4,41): error CS8503: The modifier 'public' is not valid for this item in C# 7. Please use language version 7.1 or greater.
                //     public abstract event System.Action P1; 
                Diagnostic(ErrorCode.ERR_DefaultInterfaceImplementationModifier, "P1").WithArguments("public", "7", "7.1").WithLocation(4, 41)
                );

            ValidateEventModifiers_06(compilation1);
        }

        private static void ValidateEventModifiers_06(CSharpCompilation compilation1)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p1 = i1.GetMember<EventSymbol>("P1");

            Assert.True(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);

            ValidateAccessor(p1.AddMethod);
            ValidateAccessor(p1.RemoveMethod);

            void ValidateAccessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }
        }

        [Fact]
        public void EventModifiers_07()
        {
            var source1 =
@"
public interface I1
{
    public static event System.Action P1 
    {
        add
        {
            System.Console.WriteLine(""get_P1"");
        }
        remove 
        {
            System.Console.WriteLine(""set_P1"");
        }
    }

    internal static event System.Action P2 
    {
        add
        {
            System.Console.WriteLine(""get_P2"");
            P3 += value;
        }
        remove
        {
            System.Console.WriteLine(""set_P2"");
            P3 -= value;
        }
    }

    private static event System.Action P3 
    {
        add => System.Console.WriteLine(""get_P3"");
        remove => System.Console.WriteLine(""set_P3"");
    }
}

class Test1 : I1
{
    static void Main()
    {
        I1.P1 += null; 
        I1.P1 -= null;
        I1.P2 += null;
        I1.P2 -= null;
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, expectedOutput:
@"get_P1
set_P1
get_P2
get_P3
set_P2
set_P3", symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = m.GlobalNamespace.GetTypeMember("I1");

                foreach (var tuple in new[] { (name: "P1", access: Accessibility.Public),
                                              (name: "P2", access: Accessibility.Internal),
                                              (name: "P3", access: Accessibility.Private)})
                {
                    var p1 = i1.GetMember<EventSymbol>(tuple.name);

                    Assert.False(p1.IsAbstract);
                    Assert.False(p1.IsVirtual);
                    Assert.False(p1.IsSealed);
                    Assert.True(p1.IsStatic);
                    Assert.False(p1.IsExtern);
                    Assert.False(p1.IsOverride);
                    Assert.Equal(tuple.access, p1.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(p1));

                    ValidateAccessor(p1.AddMethod);
                    ValidateAccessor(p1.RemoveMethod);

                    void ValidateAccessor(MethodSymbol accessor)
                    {
                        Assert.False(accessor.IsAbstract);
                        Assert.False(accessor.IsVirtual);
                        Assert.False(accessor.IsMetadataVirtual());
                        Assert.False(accessor.IsSealed);
                        Assert.True(accessor.IsStatic);
                        Assert.False(accessor.IsExtern);
                        Assert.False(accessor.IsAsync);
                        Assert.False(accessor.IsOverride);
                        Assert.Equal(tuple.access, accessor.DeclaredAccessibility);
                        Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                    }
                }
            }

            var source2 =
@"
public interface I1
{
    public static event System.Action P1; 

    internal static event System.Action P2 
    {
        add;
        remove;
    }

    private static event System.Action P3 = null;
}

class Test1 : I1
{
    static void Main()
    {
    }
}
";
            var compilation2 = CreateStandardCompilation(source2, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(
                // (8,12): error CS0073: An add or remove accessor must have a body
                //         add;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(8, 12),
                // (9,15): error CS0073: An add or remove accessor must have a body
                //         remove;
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(9, 15),
                // (4,39): error CS0065: 'I1.P1': event property must have both add and remove accessors
                //     public static event System.Action P1; 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P1").WithArguments("I1.P1").WithLocation(4, 39),
                // (12,40): error CS0068: 'I1.P3': event in interface cannot have initializer
                //     private static event System.Action P3 = null;
                Diagnostic(ErrorCode.ERR_InterfaceEventInitializer, "P3").WithArguments("I1.P3").WithLocation(12, 40),
                // (12,40): error CS0065: 'I1.P3': event property must have both add and remove accessors
                //     private static event System.Action P3 = null;
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P3").WithArguments("I1.P3").WithLocation(12, 40),
                // (12,40): warning CS0067: The event 'I1.P3' is never used
                //     private static event System.Action P3 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "P3").WithArguments("I1.P3").WithLocation(12, 40)
                );

            Validate(compilation2.SourceModule);
        }

        [Fact]
        public void EventModifiers_08()
        {
            var source1 =
@"
public interface I1
{
    abstract static event System.Action P1; 

    virtual static event System.Action P2 {add {} remove{}} 
    
    sealed static event System.Action P3 {add; remove;}
}

class Test1 : I1
{
    event System.Action I1.P1 {add {} remove{}} 
    event System.Action I1.P2 {add {} remove{}} 
    event System.Action I1.P3 {add {} remove{}} 
}

class Test2 : I1
{}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (8,46): error CS0073: An add or remove accessor must have a body
                //     sealed static event System.Action P3 {add; remove;}
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(8, 46),
                // (8,54): error CS0073: An add or remove accessor must have a body
                //     sealed static event System.Action P3 {add; remove;}
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(8, 54),
                // (6,40): error CS0112: A static member 'I1.P2' cannot be marked as override, virtual, or abstract
                //     virtual static event System.Action P2 {add {} remove{}} 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "P2").WithArguments("I1.P2").WithLocation(6, 40),
                // (8,39): error CS0238: 'I1.P3' cannot be sealed because it is not an override
                //     sealed static event System.Action P3 {add; remove;}
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P3").WithArguments("I1.P3").WithLocation(8, 39),
                // (4,41): error CS0112: A static member 'I1.P1' cannot be marked as override, virtual, or abstract
                //     abstract static event System.Action P1; 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "P1").WithArguments("I1.P1").WithLocation(4, 41),
                // (13,28): error CS0539: 'Test1.P1' in explicit interface declaration is not found among members of the interface that can be implemented
                //     event System.Action I1.P1 {add {} remove{}} 
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P1").WithArguments("Test1.P1").WithLocation(13, 28),
                // (14,28): error CS0539: 'Test1.P2' in explicit interface declaration is not found among members of the interface that can be implemented
                //     event System.Action I1.P2 {add {} remove{}} 
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P2").WithArguments("Test1.P2").WithLocation(14, 28),
                // (15,28): error CS0539: 'Test1.P3' in explicit interface declaration is not found among members of the interface that can be implemented
                //     event System.Action I1.P3 {add {} remove{}} 
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P3").WithArguments("Test1.P3").WithLocation(15, 28)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p1 = i1.GetMember<EventSymbol>("P1");

            Assert.True(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.True(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));

            ValidateAccessor1(p1.AddMethod);
            ValidateAccessor1(p1.RemoveMethod);
            void ValidateAccessor1(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.True(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
            }

            var p2 = i1.GetMember<EventSymbol>("P2");

            Assert.False(p2.IsAbstract);
            Assert.True(p2.IsVirtual);
            Assert.False(p2.IsSealed);
            Assert.True(p2.IsStatic);
            Assert.False(p2.IsExtern);
            Assert.False(p2.IsOverride);
            Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p2));

            ValidateAccessor2(p2.AddMethod);
            ValidateAccessor2(p2.RemoveMethod);
            void ValidateAccessor2(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.True(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.True(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
            }

            var p3 = i1.GetMember<EventSymbol>("P3");

            Assert.False(p3.IsAbstract);
            Assert.False(p3.IsVirtual);
            Assert.True(p3.IsSealed);
            Assert.True(p3.IsStatic);
            Assert.False(p3.IsExtern);
            Assert.False(p3.IsOverride);
            Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p3));

            ValidateAccessor3(p3.AddMethod);
            ValidateAccessor3(p3.RemoveMethod);
            void ValidateAccessor3(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.False(accessor.IsMetadataVirtual());
                Assert.True(accessor.IsSealed);
                Assert.True(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
            }
        }

        [Fact]
        public void EventModifiers_09()
        {
            var source1 =
@"
public interface I1
{
    private event System.Action P1 
    {
        add
        { 
            System.Console.WriteLine(""get_P1"");
        }           
        remove
        { 
            System.Console.WriteLine(""set_P1"");
        }           
    }
    sealed void M()
    {
        P1 += null;
        P1 -= null;
    }
}
public interface I2
{
    private event System.Action P2 
    {
        add => System.Console.WriteLine(""get_P2"");
        remove => System.Console.WriteLine(""set_P2"");
    }

    sealed void M()
    {
        P2 += null;
        P2 -= null;
    }
}

class Test1 : I1, I2
{
    static void Main()
    {
        I1 x1 = new Test1();
        x1.M();
        I2 x2 = new Test1();
        x2.M();
    }
}
";

            ValidateEventModifiers_09(source1);
        }

        private void ValidateEventModifiers_09(string source1)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");

                for (int i = 1; i <= 2; i++)
                {
                    var i1 = m.GlobalNamespace.GetTypeMember("I" + i);
                    var p1 = GetSingleEvent(i1);

                    Assert.False(p1.IsAbstract);
                    Assert.False(p1.IsVirtual);
                    Assert.False(p1.IsSealed);
                    Assert.False(p1.IsStatic);
                    Assert.False(p1.IsExtern);
                    Assert.False(p1.IsOverride);
                    Assert.Equal(Accessibility.Private, p1.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(p1));

                    ValidateAccessor(p1.AddMethod);
                    ValidateAccessor(p1.RemoveMethod);

                    void ValidateAccessor(MethodSymbol acessor)
                    {
                        Assert.False(acessor.IsAbstract);
                        Assert.False(acessor.IsVirtual);
                        Assert.False(acessor.IsMetadataVirtual());
                        Assert.False(acessor.IsSealed);
                        Assert.False(acessor.IsStatic);
                        Assert.False(acessor.IsExtern);
                        Assert.False(acessor.IsAsync);
                        Assert.False(acessor.IsOverride);
                        Assert.Equal(Accessibility.Private, acessor.DeclaredAccessibility);
                        Assert.Null(test1.FindImplementationForInterfaceMember(acessor));
                    }
                }
            }
        }

        [Fact]
        public void EventModifiers_10()
        {
            var source1 =
@"
public interface I1
{
    abstract private event System.Action P1; 

    virtual private event System.Action P2;

    sealed private event System.Action P3 
    {
        add => throw null;
        remove {}
    }

    private event System.Action P4 = null;
}

class Test1 : I1
{
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (4,42): error CS0621: 'I1.P1': virtual or abstract members cannot be private
                //     abstract private event System.Action P1; 
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "P1").WithArguments("I1.P1").WithLocation(4, 42),
                // (6,41): error CS0065: 'I1.P2': event property must have both add and remove accessors
                //     virtual private event System.Action P2;
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P2").WithArguments("I1.P2").WithLocation(6, 41),
                // (6,41): error CS0621: 'I1.P2': virtual or abstract members cannot be private
                //     virtual private event System.Action P2;
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "P2").WithArguments("I1.P2").WithLocation(6, 41),
                // (8,40): error CS0238: 'I1.P3' cannot be sealed because it is not an override
                //     sealed private event System.Action P3 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P3").WithArguments("I1.P3").WithLocation(8, 40),
                // (14,33): error CS0068: 'I1.P4': event in interface cannot have initializer
                //     private event System.Action P4 = null;
                Diagnostic(ErrorCode.ERR_InterfaceEventInitializer, "P4").WithArguments("I1.P4").WithLocation(14, 33),
                // (14,33): error CS0065: 'I1.P4': event property must have both add and remove accessors
                //     private event System.Action P4 = null;
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P4").WithArguments("I1.P4").WithLocation(14, 33),
                // (17,15): error CS0535: 'Test1' does not implement interface member 'I1.P1'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.P1"),
                // (14,33): warning CS0067: The event 'I1.P4' is never used
                //     private event System.Action P4 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "P4").WithArguments("I1.P4").WithLocation(14, 33)
                );

            ValidateEventModifiers_10(compilation1);
        }

        private static void ValidateEventModifiers_10(CSharpCompilation compilation1)
        {
            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p1 = i1.GetMembers().OfType<EventSymbol>().ElementAt(0);

            Assert.True(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Private, p1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));

            ValidateP1Accessor(p1.AddMethod);
            ValidateP1Accessor(p1.RemoveMethod);
            void ValidateP1Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Private, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
            }

            var p2 = i1.GetMembers().OfType<EventSymbol>().ElementAt(1);

            Assert.False(p2.IsAbstract);
            Assert.True(p2.IsVirtual);
            Assert.False(p2.IsSealed);
            Assert.False(p2.IsStatic);
            Assert.False(p2.IsExtern);
            Assert.False(p2.IsOverride);
            Assert.Equal(Accessibility.Private, p2.DeclaredAccessibility);
            Assert.Same(p2, test1.FindImplementationForInterfaceMember(p2));

            ValidateP2Accessor(p2.AddMethod);
            ValidateP2Accessor(p2.RemoveMethod);
            void ValidateP2Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.True(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Private, accessor.DeclaredAccessibility);
                Assert.Same(accessor, test1.FindImplementationForInterfaceMember(accessor));
            }

            var p3 = i1.GetMembers().OfType<EventSymbol>().ElementAt(2);

            Assert.False(p3.IsAbstract);
            Assert.False(p3.IsVirtual);
            Assert.True(p3.IsSealed);
            Assert.False(p3.IsStatic);
            Assert.False(p3.IsExtern);
            Assert.False(p3.IsOverride);
            Assert.Equal(Accessibility.Private, p3.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p3));

            ValidateP3Accessor(p3.AddMethod);
            ValidateP3Accessor(p3.RemoveMethod);
            void ValidateP3Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.False(accessor.IsMetadataVirtual());
                Assert.True(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Private, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
            }

            var p4 = i1.GetMembers().OfType<EventSymbol>().ElementAt(3);

            Assert.False(p4.IsAbstract);
            Assert.False(p4.IsVirtual);
            Assert.False(p4.IsSealed);
            Assert.False(p4.IsStatic);
            Assert.False(p4.IsExtern);
            Assert.False(p4.IsOverride);
            Assert.Equal(Accessibility.Private, p4.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p4));

            ValidateP4Accessor(p4.AddMethod);
            ValidateP4Accessor(p4.RemoveMethod);
            void ValidateP4Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.False(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Private, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
            }
        }

        [Fact]
        public void EventModifiers_11()
        {
            var source1 =
@"
public interface I1
{
    internal abstract event System.Action P1; 

    sealed void Test()
    {
        P1 += null;
        P1 -= null;
    }
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.Test();
    }

    public event System.Action P1 
    {
        add
        {
            System.Console.WriteLine(""get_P1"");
        }
        remove
        {
            System.Console.WriteLine(""set_P1"");
        }
    }
}
";

            ValidateEventModifiers_11(source1, source2,
                // (2,15): error CS0535: 'Test2' does not implement interface member 'I1.P1'
                // class Test2 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.P1").WithLocation(2, 15)
                );
        }

        private void ValidateEventModifiers_11(string source1, string source2, params DiagnosticDescription[] expected)
        {
            var compilation1 = CreateStandardCompilation(source1 + source2, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            Validate1(compilation1.SourceModule);

            void Validate1(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = test1.Interfaces.Single();
                var p1 = GetSingleEvent(i1);
                var test1P1 = GetSingleEvent(test1);
                var p1add = p1.AddMethod;
                var p1remove = p1.RemoveMethod;

                ValidateEvent(p1);
                ValidateMethod(p1add);
                ValidateMethod(p1remove);
                Assert.Same(test1P1, test1.FindImplementationForInterfaceMember(p1));
                Assert.Same(test1P1.AddMethod, test1.FindImplementationForInterfaceMember(p1add));
                Assert.Same(test1P1.RemoveMethod, test1.FindImplementationForInterfaceMember(p1remove));
            }

            void ValidateEvent(EventSymbol p1)
            {
                Assert.True(p1.IsAbstract);
                Assert.False(p1.IsVirtual);
                Assert.False(p1.IsSealed);
                Assert.False(p1.IsStatic);
                Assert.False(p1.IsExtern);
                Assert.False(p1.IsOverride);
                Assert.Equal(Accessibility.Internal, p1.DeclaredAccessibility);
            }

            void ValidateMethod(MethodSymbol m1)
            {
                Assert.True(m1.IsAbstract);
                Assert.False(m1.IsVirtual);
                Assert.True(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(Accessibility.Internal, m1.DeclaredAccessibility);
            }

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            {
                var i1 = compilation2.GetTypeByMetadataName("I1");
                var p1 = GetSingleEvent(i1);
                ValidateEvent(p1);
                ValidateMethod(p1.AddMethod);
                ValidateMethod(p1.RemoveMethod);
            }

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation2.ToMetadataReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate1);

            Validate1(compilation3.SourceModule);

            var compilation4 = CreateStandardCompilation(source2, new[] { compilation2.EmitToImageReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation4.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation4, verify: false, symbolValidator: Validate1);

            Validate1(compilation4.SourceModule);

            var source3 =
@"
class Test2 : I1
{
}
";

            var compilation5 = CreateStandardCompilation(source3, new[] { compilation2.ToMetadataReference() }, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation5.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation5.VerifyDiagnostics(expected);

            {
                var test2 = compilation5.GetTypeByMetadataName("Test2");
                var i1 = compilation5.GetTypeByMetadataName("I1");
                var p1 = GetSingleEvent(i1);
                Assert.Null(test2.FindImplementationForInterfaceMember(p1));
                Assert.Null(test2.FindImplementationForInterfaceMember(p1.AddMethod));
                Assert.Null(test2.FindImplementationForInterfaceMember(p1.RemoveMethod));
            }

            var compilation6 = CreateStandardCompilation(source3, new[] { compilation2.EmitToImageReference() }, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation6.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation6.VerifyDiagnostics(expected);

            {
                var test2 = compilation6.GetTypeByMetadataName("Test2");
                var i1 = compilation6.GetTypeByMetadataName("I1");
                var p1 = GetSingleEvent(i1);
                Assert.Null(test2.FindImplementationForInterfaceMember(p1));
                Assert.Null(test2.FindImplementationForInterfaceMember(p1.AddMethod));
                Assert.Null(test2.FindImplementationForInterfaceMember(p1.RemoveMethod));
            }
        }

        [Fact]
        public void EventModifiers_12()
        {
            var source1 =
@"
public interface I1
{
    internal abstract event System.Action P1; 
}

class Test1 : I1
{
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (7,15): error CS0535: 'Test1' does not implement interface member 'I1.P1'
                // class Test1 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.P1").WithLocation(7, 15)
                );

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var p1 = i1.GetMember<EventSymbol>("P1");
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));
            Assert.Null(test1.FindImplementationForInterfaceMember(p1.AddMethod));
            Assert.Null(test1.FindImplementationForInterfaceMember(p1.RemoveMethod));
        }

        [Fact]
        public void EventModifiers_13()
        {
            var source1 =
@"
public interface I1
{
    public sealed event System.Action P1 
    {
        add
        { 
            System.Console.WriteLine(""get_P1"");
        }           
        remove
        { 
            System.Console.WriteLine(""set_P1"");
        }           
    }
}
public interface I2
{
    public sealed event System.Action P2 
    {
        add => System.Console.WriteLine(""get_P2"");
        remove => System.Console.WriteLine(""set_P2"");
    }
}

class Test1 : I1
{
    static void Main()
    {
        I1 i1 = new Test1();
        i1.P1 += null;
        i1.P1 -= null;
        I2 i2 = new Test2();
        i2.P2 += null;
        i2.P2 -= null;
    }

    public event System.Action P1 
    {
        add => throw null;          
        remove => throw null;         
    }
}
class Test2 : I2
{
    public event System.Action P2 
    {
        add => throw null;          
        remove => throw null;         
    }
}
";

            ValidateEventModifiers_13(source1);
        }

        private void ValidateEventModifiers_13(string source1)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);

            void Validate(ModuleSymbol m)
            {
                for (int i = 1; i <= 2; i++)
                {
                    var test1 = m.GlobalNamespace.GetTypeMember("Test" + i);
                    var i1 = m.GlobalNamespace.GetTypeMember("I" + i);
                    var p1 = GetSingleEvent(i1);

                    Assert.False(p1.IsAbstract);
                    Assert.False(p1.IsVirtual);
                    Assert.False(p1.IsSealed);
                    Assert.False(p1.IsStatic);
                    Assert.False(p1.IsExtern);
                    Assert.False(p1.IsOverride);
                    Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(p1));

                    ValidateAccessor(p1.AddMethod);
                    ValidateAccessor(p1.RemoveMethod);

                    void ValidateAccessor(MethodSymbol acessor)
                    {
                        Assert.False(acessor.IsAbstract);
                        Assert.False(acessor.IsVirtual);
                        Assert.False(acessor.IsMetadataVirtual());
                        Assert.False(acessor.IsSealed);
                        Assert.False(acessor.IsStatic);
                        Assert.False(acessor.IsExtern);
                        Assert.False(acessor.IsAsync);
                        Assert.False(acessor.IsOverride);
                        Assert.Equal(Accessibility.Public, acessor.DeclaredAccessibility);
                        Assert.Null(test1.FindImplementationForInterfaceMember(acessor));
                    }
                }
            }

            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate);
            Validate(compilation1.SourceModule);
        }

        [Fact]
        public void EventModifiers_14()
        {
            var source1 =
@"
public interface I1
{
    public sealed event System.Action P1 = null; 
}
public interface I2
{
    abstract sealed event System.Action P2 {add; remove;} 
}
public interface I3
{
    virtual sealed event System.Action P3 
    {
        add {}
        remove {}
    }
}

class Test1 : I1, I2, I3
{
    event System.Action I1.P1 { add => throw null; remove => throw null; }
    event System.Action I2.P2 { add => throw null; remove => throw null; }
    event System.Action I3.P3 { add => throw null; remove => throw null; }
}

class Test2 : I1, I2, I3
{}
";
            ValidateEventModifiers_14(source1,
                // (4,39): error CS0068: 'I1.P1': event in interface cannot have initializer
                //     public sealed event System.Action P1 = null; 
                Diagnostic(ErrorCode.ERR_InterfaceEventInitializer, "P1").WithArguments("I1.P1").WithLocation(4, 39),
                // (4,39): error CS0065: 'I1.P1': event property must have both add and remove accessors
                //     public sealed event System.Action P1; 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P1").WithArguments("I1.P1").WithLocation(4, 39),
                // (8,41): error CS0238: 'I2.P2' cannot be sealed because it is not an override
                //     abstract sealed event System.Action P2 {add; remove;} 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P2").WithArguments("I2.P2").WithLocation(8, 41),
                // (8,48): error CS0073: An add or remove accessor must have a body
                //     abstract sealed event System.Action P2 {add; remove;} 
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(8, 48),
                // (8,56): error CS0073: An add or remove accessor must have a body
                //     abstract sealed event System.Action P2 {add; remove;} 
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(8, 56),
                // (12,40): error CS0238: 'I3.P3' cannot be sealed because it is not an override
                //     virtual sealed event System.Action P3 
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P3").WithArguments("I3.P3").WithLocation(12, 40),
                // (21,28): error CS0539: 'Test1.P1' in explicit interface declaration is not found among members of the interface that can be implemented
                //     event System.Action I1.P1 { add => throw null; remove => throw null; }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P1").WithArguments("Test1.P1").WithLocation(21, 28),
                // (26,19): error CS0535: 'Test2' does not implement interface member 'I2.P2'
                // class Test2 : I1, I2, I3
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Test2", "I2.P2").WithLocation(26, 19),
                // (4,39): warning CS0067: The event 'I1.P1' is never used
                //     public sealed event System.Action P1 = null; 
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "P1").WithArguments("I1.P1").WithLocation(4, 39)
                );
        }

        private void ValidateEventModifiers_14(string source1, params DiagnosticDescription[] expected)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(expected);

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");
            var p1 = GetSingleEvent(compilation1, "I1");

            Assert.False(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));
            Assert.Null(test2.FindImplementationForInterfaceMember(p1));

            Validate1(p1.AddMethod);
            Validate1(p1.RemoveMethod);
            void Validate1(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.False(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
            }

            var p2 = GetSingleEvent(compilation1, "I2");
            var test1P2 = test1.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I2.")).Single();

            Assert.True(p2.IsAbstract);
            Assert.False(p2.IsVirtual);
            Assert.True(p2.IsSealed);
            Assert.False(p2.IsStatic);
            Assert.False(p2.IsExtern);
            Assert.False(p2.IsOverride);
            Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
            Assert.Same(test1P2, test1.FindImplementationForInterfaceMember(p2));
            Assert.Null(test2.FindImplementationForInterfaceMember(p2));

            Validate2(p2.AddMethod, test1P2.AddMethod);
            Validate2(p2.RemoveMethod, test1P2.RemoveMethod);
            void Validate2(MethodSymbol accessor, MethodSymbol implementation)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.True(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Same(implementation, test1.FindImplementationForInterfaceMember(accessor));
                Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
            }

            var p3 = GetSingleEvent(compilation1, "I3");
            var test1P3 = test1.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I3.")).Single();

            Assert.False(p3.IsAbstract);
            Assert.True(p3.IsVirtual);
            Assert.True(p3.IsSealed);
            Assert.False(p3.IsStatic);
            Assert.False(p3.IsExtern);
            Assert.False(p3.IsOverride);
            Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
            Assert.Same(test1P3, test1.FindImplementationForInterfaceMember(p3));
            Assert.Same(p3, test2.FindImplementationForInterfaceMember(p3));

            Validate3(p3.AddMethod, test1P3.AddMethod);
            Validate3(p3.RemoveMethod, test1P3.RemoveMethod);
            void Validate3(MethodSymbol accessor, MethodSymbol implementation)
            {
                Assert.False(accessor.IsAbstract);
                Assert.True(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.True(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Same(implementation, test1.FindImplementationForInterfaceMember(accessor));
                Assert.Same(accessor, test2.FindImplementationForInterfaceMember(accessor));
            }
        }

        [Fact]
        public void EventModifiers_15()
        {
            var source1 =
@"
public interface I0
{
    abstract virtual event System.Action P0;
}
public interface I1
{
    abstract virtual event System.Action P1 { add { throw null; } }
}
public interface I2
{
    virtual abstract event System.Action P2 
    {
        add { throw null; }
        remove { throw null; }
    }
}
public interface I3
{
    abstract virtual event System.Action P3 { remove { throw null; } }
}
public interface I4
{
    abstract virtual event System.Action P4 { add => throw null; }
}
public interface I5
{
    abstract virtual event System.Action P5 
    {
        add => throw null;
        remove => throw null;
    }
}
public interface I6
{
    abstract virtual event System.Action P6 { remove => throw null; }
}
public interface I7
{
    abstract virtual event System.Action P7 { add; }
}
public interface I8
{
    abstract virtual event System.Action P8 { remove; } 
}

class Test1 : I0, I1, I2, I3, I4, I5, I6, I7, I8
{
    event System.Action I0.P0 
    {
        add { throw null; }
        remove { throw null; }
    }
    event System.Action I1.P1 
    {
        add { throw null; }
    }
    event System.Action I2.P2 
    {
        add { throw null; }
        remove { throw null; }
    }
    event System.Action I3.P3 
    {
        remove { throw null; }
    }
    event System.Action I4.P4 
    {
        add { throw null; }
    }
    event System.Action I5.P5 
    {
        add { throw null; }
        remove { throw null; }
    }
    event System.Action I6.P6 
    {
        remove { throw null; }
    }
    event System.Action I7.P7 
    {
        add { throw null; }
    }
    event System.Action I8.P8 
    {
        remove { throw null; }
    }
}

class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
{}
";
            ValidateEventModifiers_15(source1,
                // (4,42): error CS0503: The abstract method 'I0.P0' cannot be marked virtual
                //     abstract virtual event System.Action P0;
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P0").WithArguments("I0.P0").WithLocation(4, 42),
                // (8,42): error CS0065: 'I1.P1': event property must have both add and remove accessors
                //     abstract virtual event System.Action P1 { add { throw null; } }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P1").WithArguments("I1.P1").WithLocation(8, 42),
                // (8,42): error CS0503: The abstract method 'I1.P1' cannot be marked virtual
                //     abstract virtual event System.Action P1 { add { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P1").WithArguments("I1.P1").WithLocation(8, 42),
                // (8,47): error CS0500: 'I1.P1.add' cannot declare a body because it is marked abstract
                //     abstract virtual event System.Action P1 { add { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "add").WithArguments("I1.P1.add").WithLocation(8, 47),
                // (12,42): error CS0503: The abstract method 'I2.P2' cannot be marked virtual
                //     virtual abstract event System.Action P2 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P2").WithArguments("I2.P2").WithLocation(12, 42),
                // (14,9): error CS0500: 'I2.P2.add' cannot declare a body because it is marked abstract
                //         add { throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "add").WithArguments("I2.P2.add").WithLocation(14, 9),
                // (15,9): error CS0500: 'I2.P2.remove' cannot declare a body because it is marked abstract
                //         remove { throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "remove").WithArguments("I2.P2.remove").WithLocation(15, 9),
                // (20,42): error CS0065: 'I3.P3': event property must have both add and remove accessors
                //     abstract virtual event System.Action P3 { remove { throw null; } }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P3").WithArguments("I3.P3").WithLocation(20, 42),
                // (20,42): error CS0503: The abstract method 'I3.P3' cannot be marked virtual
                //     abstract virtual event System.Action P3 { remove { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P3").WithArguments("I3.P3").WithLocation(20, 42),
                // (20,47): error CS0500: 'I3.P3.remove' cannot declare a body because it is marked abstract
                //     abstract virtual event System.Action P3 { remove { throw null; } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "remove").WithArguments("I3.P3.remove").WithLocation(20, 47),
                // (24,42): error CS0065: 'I4.P4': event property must have both add and remove accessors
                //     abstract virtual event System.Action P4 { add => throw null; }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P4").WithArguments("I4.P4").WithLocation(24, 42),
                // (24,42): error CS0503: The abstract method 'I4.P4' cannot be marked virtual
                //     abstract virtual event System.Action P4 { add => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P4").WithArguments("I4.P4").WithLocation(24, 42),
                // (24,47): error CS0500: 'I4.P4.add' cannot declare a body because it is marked abstract
                //     abstract virtual event System.Action P4 { add => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "add").WithArguments("I4.P4.add").WithLocation(24, 47),
                // (28,42): error CS0503: The abstract method 'I5.P5' cannot be marked virtual
                //     abstract virtual event System.Action P5 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P5").WithArguments("I5.P5").WithLocation(28, 42),
                // (30,9): error CS0500: 'I5.P5.add' cannot declare a body because it is marked abstract
                //         add => throw null;
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "add").WithArguments("I5.P5.add").WithLocation(30, 9),
                // (31,9): error CS0500: 'I5.P5.remove' cannot declare a body because it is marked abstract
                //         remove => throw null;
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "remove").WithArguments("I5.P5.remove").WithLocation(31, 9),
                // (36,42): error CS0065: 'I6.P6': event property must have both add and remove accessors
                //     abstract virtual event System.Action P6 { remove => throw null; }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P6").WithArguments("I6.P6").WithLocation(36, 42),
                // (36,42): error CS0503: The abstract method 'I6.P6' cannot be marked virtual
                //     abstract virtual event System.Action P6 { remove => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P6").WithArguments("I6.P6").WithLocation(36, 42),
                // (36,47): error CS0500: 'I6.P6.remove' cannot declare a body because it is marked abstract
                //     abstract virtual event System.Action P6 { remove => throw null; }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "remove").WithArguments("I6.P6.remove").WithLocation(36, 47),
                // (40,42): error CS0065: 'I7.P7': event property must have both add and remove accessors
                //     abstract virtual event System.Action P7 { add; }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P7").WithArguments("I7.P7").WithLocation(40, 42),
                // (40,42): error CS0503: The abstract method 'I7.P7' cannot be marked virtual
                //     abstract virtual event System.Action P7 { add; }
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P7").WithArguments("I7.P7").WithLocation(40, 42),
                // (40,50): error CS0073: An add or remove accessor must have a body
                //     abstract virtual event System.Action P7 { add; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(40, 50),
                // (44,42): error CS0065: 'I8.P8': event property must have both add and remove accessors
                //     abstract virtual event System.Action P8 { remove; } 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P8").WithArguments("I8.P8").WithLocation(44, 42),
                // (44,42): error CS0503: The abstract method 'I8.P8' cannot be marked virtual
                //     abstract virtual event System.Action P8 { remove; } 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, "P8").WithArguments("I8.P8").WithLocation(44, 42),
                // (44,53): error CS0073: An add or remove accessor must have a body
                //     abstract virtual event System.Action P8 { remove; } 
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";").WithLocation(44, 53),
                // (54,28): error CS0065: 'Test1.I1.P1': event property must have both add and remove accessors
                //     event System.Action I1.P1 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P1").WithArguments("Test1.I1.P1").WithLocation(54, 28),
                // (63,28): error CS0065: 'Test1.I3.P3': event property must have both add and remove accessors
                //     event System.Action I3.P3 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P3").WithArguments("Test1.I3.P3").WithLocation(63, 28),
                // (67,28): error CS0065: 'Test1.I4.P4': event property must have both add and remove accessors
                //     event System.Action I4.P4 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P4").WithArguments("Test1.I4.P4").WithLocation(67, 28),
                // (76,28): error CS0065: 'Test1.I6.P6': event property must have both add and remove accessors
                //     event System.Action I6.P6 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P6").WithArguments("Test1.I6.P6").WithLocation(76, 28),
                // (80,28): error CS0065: 'Test1.I7.P7': event property must have both add and remove accessors
                //     event System.Action I7.P7 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P7").WithArguments("Test1.I7.P7").WithLocation(80, 28),
                // (84,28): error CS0065: 'Test1.I8.P8': event property must have both add and remove accessors
                //     event System.Action I8.P8 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "P8").WithArguments("Test1.I8.P8").WithLocation(84, 28),
                // (90,15): error CS0535: 'Test2' does not implement interface member 'I0.P0'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I0").WithArguments("Test2", "I0.P0").WithLocation(90, 15),
                // (90,19): error CS0535: 'Test2' does not implement interface member 'I1.P1'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test2", "I1.P1").WithLocation(90, 19),
                // (90,23): error CS0535: 'Test2' does not implement interface member 'I2.P2'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Test2", "I2.P2").WithLocation(90, 23),
                // (90,27): error CS0535: 'Test2' does not implement interface member 'I3.P3'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I3").WithArguments("Test2", "I3.P3").WithLocation(90, 27),
                // (90,31): error CS0535: 'Test2' does not implement interface member 'I4.P4'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I4").WithArguments("Test2", "I4.P4").WithLocation(90, 31),
                // (90,35): error CS0535: 'Test2' does not implement interface member 'I5.P5'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I5").WithArguments("Test2", "I5.P5").WithLocation(90, 35),
                // (90,39): error CS0535: 'Test2' does not implement interface member 'I6.P6'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I6").WithArguments("Test2", "I6.P6").WithLocation(90, 39),
                // (90,43): error CS0535: 'Test2' does not implement interface member 'I7.P7'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I7").WithArguments("Test2", "I7.P7").WithLocation(90, 43),
                // (90,47): error CS0535: 'Test2' does not implement interface member 'I8.P8'
                // class Test2 : I0, I1, I2, I3, I4, I5, I6, I7, I8
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I8").WithArguments("Test2", "I8.P8").WithLocation(90, 47)
                );
        }

        private void ValidateEventModifiers_15(string source1, params DiagnosticDescription[] expected)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(expected);

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");

            for (int i = 0; i <= 8; i++)
            {
                var i1 = compilation1.GetTypeByMetadataName("I" + i);
                var p2 = GetSingleEvent(i1);
                var test1P2 = test1.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith(i1.Name)).Single();

                Assert.True(p2.IsAbstract);
                Assert.True(p2.IsVirtual);
                Assert.False(p2.IsSealed);
                Assert.False(p2.IsStatic);
                Assert.False(p2.IsExtern);
                Assert.False(p2.IsOverride);
                Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
                Assert.Same(test1P2, test1.FindImplementationForInterfaceMember(p2));
                Assert.Null(test2.FindImplementationForInterfaceMember(p2));

                switch (i)
                {
                    case 3:
                    case 6:
                    case 8:
                        Assert.Null(p2.AddMethod);
                        ValidateAccessor(p2.RemoveMethod, test1P2.RemoveMethod);
                        break;
                    case 1:
                    case 4:
                    case 7:
                        Assert.Null(p2.RemoveMethod);
                        ValidateAccessor(p2.AddMethod, test1P2.AddMethod);
                        break;
                    default:
                        ValidateAccessor(p2.AddMethod, test1P2.AddMethod);
                        ValidateAccessor(p2.RemoveMethod, test1P2.RemoveMethod);
                        break;
                }

                void ValidateAccessor(MethodSymbol accessor, MethodSymbol implementedBy)
                {
                    Assert.True(accessor.IsAbstract);
                    Assert.True(accessor.IsVirtual);
                    Assert.True(accessor.IsMetadataVirtual());
                    Assert.False(accessor.IsSealed);
                    Assert.False(accessor.IsStatic);
                    Assert.False(accessor.IsExtern);
                    Assert.False(accessor.IsAsync);
                    Assert.False(accessor.IsOverride);
                    Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                    Assert.Same(implementedBy, test1.FindImplementationForInterfaceMember(accessor));
                    Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
                }
            }
        }

        [Fact]
        public void EventModifiers_16()
        {
            var source1 =
@"
public interface I1
{
    extern event System.Action P1; 
}
public interface I2
{
    virtual extern event System.Action P2;
}
public interface I3
{
    static extern event System.Action P3; 
}
public interface I4
{
    private extern event System.Action P4;
}
public interface I5
{
    extern sealed event System.Action P5;
}

class Test1 : I1, I2, I3, I4, I5
{
}

class Test2 : I1, I2, I3, I4, I5
{
    event System.Action I1.P1 { add{} remove{} }
    event System.Action I2.P2 { add{} remove{} }
}
";
            ValidateEventModifiers_16(source1);
        }

        private void ValidateEventModifiers_16(string source1)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate);

            Validate(compilation1.SourceModule);

            void Validate(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var test2 = m.GlobalNamespace.GetTypeMember("Test2");
                bool isSource = !(m is PEModuleSymbol);
                var p1 = GetSingleEvent(m, "I1");
                var test2P1 = test2.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I1.")).Single();

                Assert.False(p1.IsAbstract);
                Assert.True(p1.IsVirtual);
                Assert.False(p1.IsSealed);
                Assert.False(p1.IsStatic);
                Assert.Equal(isSource, p1.IsExtern);
                Assert.False(p1.IsOverride);
                Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
                Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
                Assert.Same(test2P1, test2.FindImplementationForInterfaceMember(p1));

                ValidateP1Accessor(p1.AddMethod, test2P1.AddMethod);
                ValidateP1Accessor(p1.RemoveMethod, test2P1.RemoveMethod);
                void ValidateP1Accessor(MethodSymbol accessor, MethodSymbol implementation)
                {
                    Assert.False(accessor.IsAbstract);
                    Assert.True(accessor.IsVirtual);
                    Assert.True(accessor.IsMetadataVirtual());
                    Assert.False(accessor.IsSealed);
                    Assert.False(accessor.IsStatic);
                    Assert.Equal(isSource, accessor.IsExtern);
                    Assert.False(accessor.IsAsync);
                    Assert.False(accessor.IsOverride);
                    Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                    Assert.Same(accessor, test1.FindImplementationForInterfaceMember(accessor));
                    Assert.Same(implementation, test2.FindImplementationForInterfaceMember(accessor));
                }

                var p2 = GetSingleEvent(m, "I2");
                var test2P2 = test2.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I2.")).Single();

                Assert.False(p2.IsAbstract);
                Assert.True(p2.IsVirtual);
                Assert.False(p2.IsSealed);
                Assert.False(p2.IsStatic);
                Assert.Equal(isSource, p2.IsExtern);
                Assert.False(p2.IsOverride);
                Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
                Assert.Same(p2, test1.FindImplementationForInterfaceMember(p2));
                Assert.Same(test2P2, test2.FindImplementationForInterfaceMember(p2));

                ValidateP2Accessor(p2.AddMethod, test2P2.AddMethod);
                ValidateP2Accessor(p2.RemoveMethod, test2P2.RemoveMethod);
                void ValidateP2Accessor(MethodSymbol accessor, MethodSymbol implementation)
                {
                    Assert.False(accessor.IsAbstract);
                    Assert.True(accessor.IsVirtual);
                    Assert.True(accessor.IsMetadataVirtual());
                    Assert.False(accessor.IsSealed);
                    Assert.False(accessor.IsStatic);
                    Assert.Equal(isSource, accessor.IsExtern);
                    Assert.False(accessor.IsAsync);
                    Assert.False(accessor.IsOverride);
                    Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                    Assert.Same(accessor, test1.FindImplementationForInterfaceMember(accessor));
                    Assert.Same(implementation, test2.FindImplementationForInterfaceMember(accessor));
                }

                var i3 = m.ContainingAssembly.GetTypeByMetadataName("I3");
                var p3 = GetSingleEvent(i3);

                Assert.False(p3.IsAbstract);
                Assert.False(p3.IsVirtual);
                Assert.False(p3.IsSealed);
                Assert.True(p3.IsStatic);
                Assert.Equal(isSource, p3.IsExtern);
                Assert.False(p3.IsOverride);
                Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(p3));
                Assert.Null(test2.FindImplementationForInterfaceMember(p3));

                ValidateP3Accessor(p3.AddMethod);
                ValidateP3Accessor(p3.RemoveMethod);
                void ValidateP3Accessor(MethodSymbol accessor)
                {
                    Assert.False(accessor.IsAbstract);
                    Assert.False(accessor.IsVirtual);
                    Assert.False(accessor.IsMetadataVirtual());
                    Assert.False(accessor.IsSealed);
                    Assert.True(accessor.IsStatic);
                    Assert.Equal(isSource, accessor.IsExtern);
                    Assert.False(accessor.IsAsync);
                    Assert.False(accessor.IsOverride);
                    Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                    Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
                }

                var p4 = GetSingleEvent(m, "I4");

                Assert.False(p4.IsAbstract);
                Assert.False(p4.IsVirtual);
                Assert.False(p4.IsSealed);
                Assert.False(p4.IsStatic);
                Assert.Equal(isSource, p4.IsExtern);
                Assert.False(p4.IsOverride);
                Assert.Equal(Accessibility.Private, p4.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(p4));
                Assert.Null(test2.FindImplementationForInterfaceMember(p4));

                ValidateP4Accessor(p4.AddMethod);
                ValidateP4Accessor(p4.RemoveMethod);
                void ValidateP4Accessor(MethodSymbol accessor)
                {
                    Assert.False(accessor.IsAbstract);
                    Assert.False(accessor.IsVirtual);
                    Assert.False(accessor.IsMetadataVirtual());
                    Assert.False(accessor.IsSealed);
                    Assert.False(accessor.IsStatic);
                    Assert.Equal(isSource, accessor.IsExtern);
                    Assert.False(accessor.IsAsync);
                    Assert.False(accessor.IsOverride);
                    Assert.Equal(Accessibility.Private, accessor.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                    Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
                }

                var p5 = GetSingleEvent(m, "I5");

                Assert.False(p5.IsAbstract);
                Assert.False(p5.IsVirtual);
                Assert.False(p5.IsSealed);
                Assert.False(p5.IsStatic);
                Assert.Equal(isSource, p5.IsExtern);
                Assert.False(p5.IsOverride);
                Assert.Equal(Accessibility.Public, p5.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(p5));
                Assert.Null(test2.FindImplementationForInterfaceMember(p5));

                ValidateP5Accessor(p5.AddMethod);
                ValidateP5Accessor(p5.RemoveMethod);
                void ValidateP5Accessor(MethodSymbol accessor)
                {
                    Assert.False(accessor.IsAbstract);
                    Assert.False(accessor.IsVirtual);
                    Assert.False(accessor.IsMetadataVirtual());
                    Assert.False(accessor.IsSealed);
                    Assert.False(accessor.IsStatic);
                    Assert.Equal(isSource, accessor.IsExtern);
                    Assert.False(accessor.IsAsync);
                    Assert.False(accessor.IsOverride);
                    Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                    Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                    Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
                }
            }
        }

        [Fact]
        public void EventModifiers_17()
        {
            var source1 =
@"
public interface I1
{
    abstract extern event System.Action P1; 
}
public interface I2
{
    extern event System.Action P2 = null; 
}
public interface I3
{
    static extern event System.Action P3 {add => throw null; remove => throw null;} 
}
public interface I4
{
    private extern event System.Action P4 { add {throw null;} remove {throw null;}}
}

class Test1 : I1, I2, I3, I4
{
}

class Test2 : I1, I2, I3, I4
{
    event System.Action I1.P1 { add => throw null; remove => throw null;}
    event System.Action I2.P2 { add => throw null; remove => throw null;}
    event System.Action I3.P3 { add => throw null; remove => throw null;}
    event System.Action I4.P4 { add => throw null; remove => throw null;}
}
";
            ValidateEventModifiers_17(source1,
                // (4,41): error CS0180: 'I1.P1' cannot be both extern and abstract
                //     abstract extern event System.Action P1; 
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "P1").WithArguments("I1.P1").WithLocation(4, 41),
                // (8,32): error CS0068: 'I2.P2': event in interface cannot have initializer
                //     extern event System.Action P2 = null; 
                Diagnostic(ErrorCode.ERR_InterfaceEventInitializer, "P2").WithArguments("I2.P2").WithLocation(8, 32),
                // (12,43): error CS0179: 'I3.P3.add' cannot be extern and declare a body
                //     static extern event System.Action P3 {add => throw null; remove => throw null;} 
                Diagnostic(ErrorCode.ERR_ExternHasBody, "add").WithArguments("I3.P3.add").WithLocation(12, 43),
                // (12,62): error CS0179: 'I3.P3.remove' cannot be extern and declare a body
                //     static extern event System.Action P3 {add => throw null; remove => throw null;} 
                Diagnostic(ErrorCode.ERR_ExternHasBody, "remove").WithArguments("I3.P3.remove").WithLocation(12, 62),
                // (16,45): error CS0179: 'I4.P4.add' cannot be extern and declare a body
                //     private extern event System.Action P4 { add {throw null;} remove {throw null;}}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "add").WithArguments("I4.P4.add").WithLocation(16, 45),
                // (16,63): error CS0179: 'I4.P4.remove' cannot be extern and declare a body
                //     private extern event System.Action P4 { add {throw null;} remove {throw null;}}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "remove").WithArguments("I4.P4.remove").WithLocation(16, 63),
                // (19,15): error CS0535: 'Test1' does not implement interface member 'I1.P1'
                // class Test1 : I1, I2, I3, I4
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.P1").WithLocation(19, 15),
                // (27,28): error CS0539: 'Test2.P3' in explicit interface declaration is not found among members of the interface that can be implemented
                //     event System.Action I3.P3 { add => throw null; remove => throw null;}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P3").WithArguments("Test2.P3").WithLocation(27, 28),
                // (28,28): error CS0539: 'Test2.P4' in explicit interface declaration is not found among members of the interface that can be implemented
                //     event System.Action I4.P4 { add => throw null; remove => throw null;}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P4").WithArguments("Test2.P4").WithLocation(28, 28),
                // (8,32): warning CS0067: The event 'I2.P2' is never used
                //     extern event System.Action P2 = null; 
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "P2").WithArguments("I2.P2").WithLocation(8, 32)
                );
        }

        private void ValidateEventModifiers_17(string source1, params DiagnosticDescription[] expected)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(expected);

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");
            var p1 = GetSingleEvent(compilation1, "I1");
            var test2P1 = test2.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I1.")).Single();

            Assert.True(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.True(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(test2P1, test2.FindImplementationForInterfaceMember(p1));

            ValidateP1Accessor(p1.AddMethod, test2P1.AddMethod);
            ValidateP1Accessor(p1.RemoveMethod, test2P1.RemoveMethod);
            void ValidateP1Accessor(MethodSymbol accessor, MethodSymbol implementation)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.True(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Same(implementation, test2.FindImplementationForInterfaceMember(accessor));
            }

            var p2 = GetSingleEvent(compilation1, "I2");
            var test2P2 = test2.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I2.")).Single();

            Assert.False(p2.IsAbstract);
            Assert.True(p2.IsVirtual);
            Assert.False(p2.IsSealed);
            Assert.False(p2.IsStatic);
            Assert.True(p2.IsExtern);
            Assert.False(p2.IsOverride);
            Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
            Assert.Same(p2, test1.FindImplementationForInterfaceMember(p2));
            Assert.Same(test2P2, test2.FindImplementationForInterfaceMember(p2));

            ValidateP2Accessor(p2.AddMethod, test2P2.AddMethod);
            ValidateP2Accessor(p2.RemoveMethod, test2P2.RemoveMethod);
            void ValidateP2Accessor(MethodSymbol accessor, MethodSymbol implementation)
            {
                Assert.False(accessor.IsAbstract);
                Assert.True(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.True(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Same(accessor, test1.FindImplementationForInterfaceMember(accessor));
                Assert.Same(implementation, test2.FindImplementationForInterfaceMember(accessor));
            }

            var p3 = GetSingleEvent(compilation1, "I3");
            var test2P3 = test2.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I3.")).Single();

            Assert.False(p3.IsAbstract);
            Assert.False(p3.IsVirtual);
            Assert.False(p3.IsSealed);
            Assert.True(p3.IsStatic);
            Assert.True(p3.IsExtern);
            Assert.False(p3.IsOverride);
            Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p3));
            Assert.Null(test2.FindImplementationForInterfaceMember(p3));

            ValidateP3Accessor(p3.AddMethod);
            ValidateP3Accessor(p3.RemoveMethod);
            void ValidateP3Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.False(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.True(accessor.IsStatic);
                Assert.True(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
            }

            var p4 = GetSingleEvent(compilation1, "I4");

            Assert.False(p4.IsAbstract);
            Assert.False(p4.IsVirtual);
            Assert.False(p4.IsSealed);
            Assert.False(p4.IsStatic);
            Assert.True(p4.IsExtern);
            Assert.False(p4.IsOverride);
            Assert.Equal(Accessibility.Private, p4.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p4));
            Assert.Null(test2.FindImplementationForInterfaceMember(p4));

            ValidateP4Accessor(p4.AddMethod);
            ValidateP4Accessor(p4.RemoveMethod);
            void ValidateP4Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.False(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.True(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Private, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
            }
        }

        [Fact]
        public void EventModifiers_18()
        {
            var source1 =
@"
public interface I1
{
    abstract event System.Action P1 {add => throw null; remove => throw null;} 
}
public interface I2
{
    abstract private event System.Action P2 = null; 
}
public interface I3
{
    static extern event System.Action P3;
}
public interface I4
{
    abstract static event System.Action P4 { add {throw null;} remove {throw null;}}
}
public interface I5
{
    override sealed event System.Action P5 { add {throw null;} remove {throw null;}}
}

class Test1 : I1, I2, I3, I4, I5
{
}

class Test2 : I1, I2, I3, I4, I5
{
    event System.Action I1.P1 { add {throw null;} remove {throw null;}}
    event System.Action I2.P2 { add {throw null;} remove {throw null;}}
    event System.Action I3.P3 { add {throw null;} remove {throw null;}}
    event System.Action I4.P4 { add {throw null;} remove {throw null;}}
    event System.Action I5.P5 { add {throw null;} remove {throw null;}}
}
";
            ValidateEventModifiers_18(source1,
                // (4,38): error CS0500: 'I1.P1.add' cannot declare a body because it is marked abstract
                //     abstract event System.Action P1 {add => throw null; remove => throw null;} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "add").WithArguments("I1.P1.add").WithLocation(4, 38),
                // (4,57): error CS0500: 'I1.P1.remove' cannot declare a body because it is marked abstract
                //     abstract event System.Action P1 {add => throw null; remove => throw null;} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "remove").WithArguments("I1.P1.remove").WithLocation(4, 57),
                // (8,42): error CS0068: 'I2.P2': event in interface cannot have initializer
                //     abstract private event System.Action P2 = null; 
                Diagnostic(ErrorCode.ERR_InterfaceEventInitializer, "P2").WithArguments("I2.P2").WithLocation(8, 42),
                // (8,42): error CS0621: 'I2.P2': virtual or abstract members cannot be private
                //     abstract private event System.Action P2 = null; 
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "P2").WithArguments("I2.P2").WithLocation(8, 42),
                // (16,46): error CS0500: 'I4.P4.add' cannot declare a body because it is marked abstract
                //     abstract static event System.Action P4 { add {throw null;} remove {throw null;}}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "add").WithArguments("I4.P4.add").WithLocation(16, 46),
                // (16,64): error CS0500: 'I4.P4.remove' cannot declare a body because it is marked abstract
                //     abstract static event System.Action P4 { add {throw null;} remove {throw null;}}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "remove").WithArguments("I4.P4.remove").WithLocation(16, 64),
                // (16,41): error CS0112: A static member 'I4.P4' cannot be marked as override, virtual, or abstract
                //     abstract static event System.Action P4 { add {throw null;} remove {throw null;}}
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "P4").WithArguments("I4.P4").WithLocation(16, 41),
                // (20,41): error CS0106: The modifier 'override' is not valid for this item
                //     override sealed event System.Action P5 { add {throw null;} remove {throw null;}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P5").WithArguments("override").WithLocation(20, 41),
                // (23,15): error CS0535: 'Test1' does not implement interface member 'I1.P1'
                // class Test1 : I1, I2, I3, I4, I5
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("Test1", "I1.P1").WithLocation(23, 15),
                // (23,19): error CS0535: 'Test1' does not implement interface member 'I2.P2'
                // class Test1 : I1, I2, I3, I4, I5
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("Test1", "I2.P2").WithLocation(23, 19),
                // (31,28): error CS0539: 'Test2.P3' in explicit interface declaration is not found among members of the interface that can be implemented
                //     event System.Action I3.P3 { add {throw null;} remove {throw null;}}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P3").WithArguments("Test2.P3").WithLocation(31, 28),
                // (32,28): error CS0539: 'Test2.P4' in explicit interface declaration is not found among members of the interface that can be implemented
                //     event System.Action I4.P4 { add {throw null;} remove {throw null;}}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P4").WithArguments("Test2.P4").WithLocation(32, 28),
                // (33,28): error CS0539: 'Test2.P5' in explicit interface declaration is not found among members of the interface that can be implemented
                //     event System.Action I5.P5 { add {throw null;} remove {throw null;}}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "P5").WithArguments("Test2.P5").WithLocation(33, 28),
                // (12,39): warning CS0626: Method, operator, or accessor 'I3.P3.add' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     static extern event System.Action P3;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "P3").WithArguments("I3.P3.add").WithLocation(12, 39),
                // (12,39): warning CS0626: Method, operator, or accessor 'I3.P3.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     static extern event System.Action P3;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "P3").WithArguments("I3.P3.remove").WithLocation(12, 39),
                // (8,42): warning CS0067: The event 'I2.P2' is never used
                //     abstract private event System.Action P2 = null; 
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "P2").WithArguments("I2.P2").WithLocation(8, 42)
                );
        }

        private void ValidateEventModifiers_18(string source1, params DiagnosticDescription[] expected)
        {
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(expected);

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");
            var p1 = GetSingleEvent(compilation1, "I1");
            var test2P1 = test2.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I1.")).Single();

            Assert.True(p1.IsAbstract);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsSealed);
            Assert.False(p1.IsStatic);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsOverride);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p1));
            Assert.Same(test2P1, test2.FindImplementationForInterfaceMember(p1));

            ValidateP1Accessor(p1.AddMethod, test2P1.AddMethod);
            ValidateP1Accessor(p1.RemoveMethod, test2P1.RemoveMethod);
            void ValidateP1Accessor(MethodSymbol accessor, MethodSymbol implementation)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Same(implementation, test2.FindImplementationForInterfaceMember(accessor));
            }

            var p2 = GetSingleEvent(compilation1, "I2");
            var test2P2 = test2.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I2.")).Single();

            Assert.True(p2.IsAbstract);
            Assert.False(p2.IsVirtual);
            Assert.False(p2.IsSealed);
            Assert.False(p2.IsStatic);
            Assert.False(p2.IsExtern);
            Assert.False(p2.IsOverride);
            Assert.Equal(Accessibility.Private, p2.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p2));
            Assert.Same(test2P2, test2.FindImplementationForInterfaceMember(p2));

            ValidateP2Accessor(p2.AddMethod, test2P2.AddMethod);
            ValidateP2Accessor(p2.RemoveMethod, test2P2.RemoveMethod);
            void ValidateP2Accessor(MethodSymbol accessor, MethodSymbol implementation)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Private, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Same(implementation, test2.FindImplementationForInterfaceMember(accessor));
            }

            var p3 = GetSingleEvent(compilation1, "I3");
            var test2P3 = test2.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I3.")).Single();

            Assert.False(p3.IsAbstract);
            Assert.False(p3.IsVirtual);
            Assert.False(p3.IsSealed);
            Assert.True(p3.IsStatic);
            Assert.True(p3.IsExtern);
            Assert.False(p3.IsOverride);
            Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p3));
            Assert.Null(test2.FindImplementationForInterfaceMember(p3));

            ValidateP3Accessor(p3.AddMethod);
            ValidateP3Accessor(p3.RemoveMethod);
            void ValidateP3Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.False(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.True(accessor.IsStatic);
                Assert.True(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
            }

            var p4 = GetSingleEvent(compilation1, "I4");
            var test2P4 = test2.GetMembers().OfType<EventSymbol>().Where(p => p.Name.StartsWith("I4.")).Single();

            Assert.True(p4.IsAbstract);
            Assert.False(p4.IsVirtual);
            Assert.False(p4.IsSealed);
            Assert.True(p4.IsStatic);
            Assert.False(p4.IsExtern);
            Assert.False(p4.IsOverride);
            Assert.Equal(Accessibility.Public, p4.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p4));
            Assert.Null(test2.FindImplementationForInterfaceMember(p4));

            ValidateP4Accessor(p4.AddMethod);
            ValidateP4Accessor(p4.RemoveMethod);
            void ValidateP4Accessor(MethodSymbol accessor)
            {
                Assert.True(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.True(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
            }

            var p5 = GetSingleEvent(compilation1, "I5");

            Assert.False(p5.IsAbstract);
            Assert.False(p5.IsVirtual);
            Assert.False(p5.IsSealed);
            Assert.False(p5.IsStatic);
            Assert.False(p5.IsExtern);
            Assert.False(p5.IsOverride);
            Assert.Equal(Accessibility.Public, p5.DeclaredAccessibility);
            Assert.Null(test1.FindImplementationForInterfaceMember(p5));
            Assert.Null(test2.FindImplementationForInterfaceMember(p5));

            ValidateP5Accessor(p5.AddMethod);
            ValidateP5Accessor(p5.RemoveMethod);
            void ValidateP5Accessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.False(accessor.IsVirtual);
                Assert.False(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
                Assert.Null(test1.FindImplementationForInterfaceMember(accessor));
                Assert.Null(test2.FindImplementationForInterfaceMember(accessor));
            }
        }

        [Fact]
        public void EventModifiers_19()
        {
            var source1 =
@"

public interface I2 {}

public interface I1
{
    public event System.Action I2.P01 {add {} remove{}}
    protected event System.Action I2.P02 {add {} remove{}}
    protected internal event System.Action I2.P03 {add {} remove{}}
    internal event System.Action I2.P04 {add {} remove{}}
    private event System.Action I2.P05 {add {} remove{}}
    static event System.Action I2.P06 {add {} remove{}}
    virtual event System.Action I2.P07 {add {} remove{}}
    sealed event System.Action I2.P08 {add {} remove{}}
    override event System.Action I2.P09 {add {} remove{}}
    abstract event System.Action I2.P10 {add {} remove{}}
    extern event System.Action I2.P11 {add {} remove{}}
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (7,35): error CS0106: The modifier 'public' is not valid for this item
                //     public event System.Action I2.P01 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P01").WithArguments("public").WithLocation(7, 35),
                // (7,35): error CS0541: 'I1.P01': explicit interface declaration can only be declared in a class or struct
                //     public event System.Action I2.P01 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P01").WithArguments("I1.P01").WithLocation(7, 35),
                // (8,38): error CS0106: The modifier 'protected' is not valid for this item
                //     protected event System.Action I2.P02 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P02").WithArguments("protected").WithLocation(8, 38),
                // (8,38): error CS0541: 'I1.P02': explicit interface declaration can only be declared in a class or struct
                //     protected event System.Action I2.P02 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P02").WithArguments("I1.P02").WithLocation(8, 38),
                // (9,47): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal event System.Action I2.P03 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P03").WithArguments("protected internal").WithLocation(9, 47),
                // (9,47): error CS0541: 'I1.P03': explicit interface declaration can only be declared in a class or struct
                //     protected internal event System.Action I2.P03 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P03").WithArguments("I1.P03").WithLocation(9, 47),
                // (10,37): error CS0106: The modifier 'internal' is not valid for this item
                //     internal event System.Action I2.P04 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P04").WithArguments("internal").WithLocation(10, 37),
                // (10,37): error CS0541: 'I1.P04': explicit interface declaration can only be declared in a class or struct
                //     internal event System.Action I2.P04 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P04").WithArguments("I1.P04").WithLocation(10, 37),
                // (11,36): error CS0106: The modifier 'private' is not valid for this item
                //     private event System.Action I2.P05 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P05").WithArguments("private").WithLocation(11, 36),
                // (11,36): error CS0541: 'I1.P05': explicit interface declaration can only be declared in a class or struct
                //     private event System.Action I2.P05 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P05").WithArguments("I1.P05").WithLocation(11, 36),
                // (12,35): error CS0106: The modifier 'static' is not valid for this item
                //     static event System.Action I2.P06 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P06").WithArguments("static").WithLocation(12, 35),
                // (12,35): error CS0541: 'I1.P06': explicit interface declaration can only be declared in a class or struct
                //     static event System.Action I2.P06 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P06").WithArguments("I1.P06").WithLocation(12, 35),
                // (13,36): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual event System.Action I2.P07 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P07").WithArguments("virtual").WithLocation(13, 36),
                // (13,36): error CS0541: 'I1.P07': explicit interface declaration can only be declared in a class or struct
                //     virtual event System.Action I2.P07 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P07").WithArguments("I1.P07").WithLocation(13, 36),
                // (14,35): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed event System.Action I2.P08 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P08").WithArguments("sealed").WithLocation(14, 35),
                // (14,35): error CS0541: 'I1.P08': explicit interface declaration can only be declared in a class or struct
                //     sealed event System.Action I2.P08 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P08").WithArguments("I1.P08").WithLocation(14, 35),
                // (15,37): error CS0106: The modifier 'override' is not valid for this item
                //     override event System.Action I2.P09 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P09").WithArguments("override").WithLocation(15, 37),
                // (15,37): error CS0541: 'I1.P09': explicit interface declaration can only be declared in a class or struct
                //     override event System.Action I2.P09 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P09").WithArguments("I1.P09").WithLocation(15, 37),
                // (16,37): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract event System.Action I2.P10 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P10").WithArguments("abstract").WithLocation(16, 37),
                // (16,37): error CS0541: 'I1.P10': explicit interface declaration can only be declared in a class or struct
                //     abstract event System.Action I2.P10 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P10").WithArguments("I1.P10").WithLocation(16, 37),
                // (17,35): error CS0106: The modifier 'extern' is not valid for this item
                //     extern event System.Action I2.P11 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P11").WithArguments("extern").WithLocation(17, 35),
                // (17,35): error CS0541: 'I1.P11': explicit interface declaration can only be declared in a class or struct
                //     extern event System.Action I2.P11 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P11").WithArguments("I1.P11").WithLocation(17, 35)
                );

            ValidateEventModifiers_19(compilation1);

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                             parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(
                // (7,35): error CS0106: The modifier 'public' is not valid for this item
                //     public event System.Action I2.P01 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P01").WithArguments("public").WithLocation(7, 35),
                // (7,35): error CS0541: 'I1.P01': explicit interface declaration can only be declared in a class or struct
                //     public event System.Action I2.P01 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P01").WithArguments("I1.P01").WithLocation(7, 35),
                // (7,40): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     public event System.Action I2.P01 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(7, 40),
                // (7,47): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     public event System.Action I2.P01 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(7, 47),
                // (8,38): error CS0106: The modifier 'protected' is not valid for this item
                //     protected event System.Action I2.P02 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P02").WithArguments("protected").WithLocation(8, 38),
                // (8,38): error CS0541: 'I1.P02': explicit interface declaration can only be declared in a class or struct
                //     protected event System.Action I2.P02 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P02").WithArguments("I1.P02").WithLocation(8, 38),
                // (8,43): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     protected event System.Action I2.P02 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(8, 43),
                // (8,50): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     protected event System.Action I2.P02 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(8, 50),
                // (9,47): error CS0106: The modifier 'protected internal' is not valid for this item
                //     protected internal event System.Action I2.P03 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P03").WithArguments("protected internal").WithLocation(9, 47),
                // (9,47): error CS0541: 'I1.P03': explicit interface declaration can only be declared in a class or struct
                //     protected internal event System.Action I2.P03 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P03").WithArguments("I1.P03").WithLocation(9, 47),
                // (9,52): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     protected internal event System.Action I2.P03 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(9, 52),
                // (9,59): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     protected internal event System.Action I2.P03 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(9, 59),
                // (10,37): error CS0106: The modifier 'internal' is not valid for this item
                //     internal event System.Action I2.P04 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P04").WithArguments("internal").WithLocation(10, 37),
                // (10,37): error CS0541: 'I1.P04': explicit interface declaration can only be declared in a class or struct
                //     internal event System.Action I2.P04 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P04").WithArguments("I1.P04").WithLocation(10, 37),
                // (10,42): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     internal event System.Action I2.P04 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(10, 42),
                // (10,49): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     internal event System.Action I2.P04 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(10, 49),
                // (11,36): error CS0106: The modifier 'private' is not valid for this item
                //     private event System.Action I2.P05 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P05").WithArguments("private").WithLocation(11, 36),
                // (11,36): error CS0541: 'I1.P05': explicit interface declaration can only be declared in a class or struct
                //     private event System.Action I2.P05 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P05").WithArguments("I1.P05").WithLocation(11, 36),
                // (11,41): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     private event System.Action I2.P05 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(11, 41),
                // (11,48): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     private event System.Action I2.P05 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(11, 48),
                // (12,35): error CS0106: The modifier 'static' is not valid for this item
                //     static event System.Action I2.P06 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P06").WithArguments("static").WithLocation(12, 35),
                // (12,35): error CS0541: 'I1.P06': explicit interface declaration can only be declared in a class or struct
                //     static event System.Action I2.P06 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P06").WithArguments("I1.P06").WithLocation(12, 35),
                // (12,40): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     static event System.Action I2.P06 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(12, 40),
                // (12,47): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     static event System.Action I2.P06 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(12, 47),
                // (13,36): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual event System.Action I2.P07 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P07").WithArguments("virtual").WithLocation(13, 36),
                // (13,36): error CS0541: 'I1.P07': explicit interface declaration can only be declared in a class or struct
                //     virtual event System.Action I2.P07 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P07").WithArguments("I1.P07").WithLocation(13, 36),
                // (13,41): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     virtual event System.Action I2.P07 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(13, 41),
                // (13,48): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     virtual event System.Action I2.P07 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(13, 48),
                // (14,35): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed event System.Action I2.P08 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P08").WithArguments("sealed").WithLocation(14, 35),
                // (14,35): error CS0541: 'I1.P08': explicit interface declaration can only be declared in a class or struct
                //     sealed event System.Action I2.P08 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P08").WithArguments("I1.P08").WithLocation(14, 35),
                // (14,40): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     sealed event System.Action I2.P08 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(14, 40),
                // (14,47): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     sealed event System.Action I2.P08 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(14, 47),
                // (15,37): error CS0106: The modifier 'override' is not valid for this item
                //     override event System.Action I2.P09 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P09").WithArguments("override").WithLocation(15, 37),
                // (15,37): error CS0541: 'I1.P09': explicit interface declaration can only be declared in a class or struct
                //     override event System.Action I2.P09 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P09").WithArguments("I1.P09").WithLocation(15, 37),
                // (15,42): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     override event System.Action I2.P09 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(15, 42),
                // (15,49): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     override event System.Action I2.P09 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(15, 49),
                // (16,37): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract event System.Action I2.P10 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P10").WithArguments("abstract").WithLocation(16, 37),
                // (16,37): error CS0541: 'I1.P10': explicit interface declaration can only be declared in a class or struct
                //     abstract event System.Action I2.P10 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P10").WithArguments("I1.P10").WithLocation(16, 37),
                // (16,42): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     abstract event System.Action I2.P10 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(16, 42),
                // (16,49): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     abstract event System.Action I2.P10 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(16, 49),
                // (17,35): error CS0106: The modifier 'extern' is not valid for this item
                //     extern event System.Action I2.P11 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P11").WithArguments("extern").WithLocation(17, 35),
                // (17,35): error CS0541: 'I1.P11': explicit interface declaration can only be declared in a class or struct
                //     extern event System.Action I2.P11 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "P11").WithArguments("I1.P11").WithLocation(17, 35),
                // (17,40): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     extern event System.Action I2.P11 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "7.1").WithLocation(17, 40),
                // (17,47): error CS8107: Feature 'default interface implementation' is not available in C# 7. Please use language version 7.1 or greater.
                //     extern event System.Action I2.P11 {add {} remove{}}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "7.1").WithLocation(17, 47)
                );

            ValidateEventModifiers_19(compilation2);
        }

        private static void ValidateEventModifiers_19(CSharpCompilation compilation1)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");

            foreach (var eventName in new[] { "I2.P01", "I2.P02", "I2.P03", "I2.P04", "I2.P05", "I2.P06",
                                                 "I2.P07", "I2.P08", "I2.P09", "I2.P10", "I2.P11"})
            {
                ValidateEventModifiers_19(i1.GetMember<EventSymbol>(eventName));
            }
        }

        private static void ValidateEventModifiers_19(EventSymbol p01)
        {
            Assert.False(p01.IsAbstract);
            Assert.True(p01.IsVirtual);
            Assert.False(p01.IsSealed);
            Assert.False(p01.IsStatic);
            Assert.False(p01.IsExtern);
            Assert.False(p01.IsOverride);
            Assert.Equal(Accessibility.Public, p01.DeclaredAccessibility);

            ValidateAccessor(p01.AddMethod);
            ValidateAccessor(p01.RemoveMethod);

            void ValidateAccessor(MethodSymbol accessor)
            {
                Assert.False(accessor.IsAbstract);
                Assert.True(accessor.IsVirtual);
                Assert.True(accessor.IsMetadataVirtual());
                Assert.False(accessor.IsSealed);
                Assert.False(accessor.IsStatic);
                Assert.False(accessor.IsExtern);
                Assert.False(accessor.IsAsync);
                Assert.False(accessor.IsOverride);
                Assert.Equal(Accessibility.Public, accessor.DeclaredAccessibility);
            }
        }

        [Fact]
        public void EventModifiers_20()
        {
            var source1 =
@"
public interface I1
{
    internal event System.Action P1
    {
        add 
        {
            System.Console.WriteLine(""get_P1"");
        }
        remove 
        {
            System.Console.WriteLine(""set_P1"");
        }
    }

    void M2() 
    {
        P1 += null;
        P1 -= null;
    }
}
";

            var source2 =
@"
class Test1 : I1
{
    static void Main()
    {
        I1 x = new Test1();
        x.M2();
    }
}
";

            ValidateEventModifiers_20(source1, source2);
        }

        private void ValidateEventModifiers_20(string source1, string source2)
        {
            var compilation1 = CreateStandardCompilation(source1 + source2, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation1, verify: false, symbolValidator: Validate1);

            Validate1(compilation1.SourceModule);

            void Validate1(ModuleSymbol m)
            {
                var test1 = m.GlobalNamespace.GetTypeMember("Test1");
                var i1 = test1.Interfaces.Single();
                var p1 = GetSingleEvent(i1);
                var p1add = p1.AddMethod;
                var p1remove = p1.RemoveMethod;

                ValidateEvent(p1);
                ValidateMethod(p1add);
                ValidateMethod(p1remove);
                Assert.Same(p1, test1.FindImplementationForInterfaceMember(p1));
                Assert.Same(p1add, test1.FindImplementationForInterfaceMember(p1add));
                Assert.Same(p1remove, test1.FindImplementationForInterfaceMember(p1remove));
            }

            void ValidateEvent(EventSymbol p1)
            {
                Assert.False(p1.IsAbstract);
                Assert.True(p1.IsVirtual);
                Assert.False(p1.IsSealed);
                Assert.False(p1.IsStatic);
                Assert.False(p1.IsExtern);
                Assert.False(p1.IsOverride);
                Assert.Equal(Accessibility.Internal, p1.DeclaredAccessibility);
            }

            void ValidateMethod(MethodSymbol m1)
            {
                Assert.False(m1.IsAbstract);
                Assert.True(m1.IsVirtual);
                Assert.True(m1.IsMetadataVirtual());
                Assert.False(m1.IsSealed);
                Assert.False(m1.IsStatic);
                Assert.False(m1.IsExtern);
                Assert.False(m1.IsAsync);
                Assert.False(m1.IsOverride);
                Assert.Equal(Accessibility.Internal, m1.DeclaredAccessibility);
            }

            var compilation2 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics();

            {
                var i1 = compilation2.GetTypeByMetadataName("I1");
                var p1 = GetSingleEvent(i1);

                ValidateEvent(p1);
                ValidateMethod(p1.AddMethod);
                ValidateMethod(p1.RemoveMethod);
            }

            var compilation3 = CreateStandardCompilation(source2, new[] { compilation2.ToMetadataReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation3.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation3, verify: false, symbolValidator: Validate1);

            Validate1(compilation3.SourceModule);

            var compilation4 = CreateStandardCompilation(source2, new[] { compilation2.EmitToImageReference() }, options: TestOptions.DebugExe,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation4.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            CompileAndVerify(compilation4, verify: false, symbolValidator: Validate1);

            Validate1(compilation4.SourceModule);
        }

        [Fact]
        public void EventModifiers_21()
        {
            var source1 =
@"
public interface I1
{
    private static event System.Action P1 { add => throw null; remove => throw null; }

    internal static event System.Action P2 { add => throw null; remove => throw null; }

    public static event System.Action P3 { add => throw null; remove => throw null; }

    static event System.Action P4 { add => throw null; remove => throw null; }
}

class Test1
{
    static void Main()
    {
        I1.P1 += null;
        I1.P1 -= null;
        I1.P2 += null;
        I1.P2 -= null;
        I1.P3 += null;
        I1.P3 -= null;
        I1.P4 += null;
        I1.P4 -= null;
    }
}
";
            var compilation1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation1.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation1.VerifyDiagnostics(
                // (17,12): error CS0122: 'I1.P1' is inaccessible due to its protection level
                //         I1.P1 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "P1").WithArguments("I1.P1").WithLocation(17, 12),
                // (18,12): error CS0122: 'I1.P1' is inaccessible due to its protection level
                //         I1.P1 -= null;
                Diagnostic(ErrorCode.ERR_BadAccess, "P1").WithArguments("I1.P1").WithLocation(18, 12)
                );

            var source2 =
@"
class Test2
{
    static void Main()
    {
        I1.P1 += null;
        I1.P1 -= null;
        I1.P2 += null;
        I1.P2 -= null;
        I1.P3 += null;
        I1.P3 -= null;
        I1.P4 += null;
        I1.P4 -= null;
    }
}
";
            var compilation2 = CreateStandardCompilation(source2, new[] { compilation1.ToMetadataReference() },
                                                         options: TestOptions.DebugDll,
                                                         parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest));
            Assert.True(compilation2.Assembly.RuntimeSupportsDefaultInterfaceImplementation);
            compilation2.VerifyDiagnostics(
                // (6,12): error CS0122: 'I1.P1' is inaccessible due to its protection level
                //         I1.P1 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "P1").WithArguments("I1.P1").WithLocation(6, 12),
                // (7,12): error CS0122: 'I1.P1' is inaccessible due to its protection level
                //         I1.P1 -= null;
                Diagnostic(ErrorCode.ERR_BadAccess, "P1").WithArguments("I1.P1").WithLocation(7, 12),
                // (8,12): error CS0122: 'I1.P2' is inaccessible due to its protection level
                //         I1.P2 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "P2").WithArguments("I1.P2").WithLocation(8, 12),
                // (9,12): error CS0122: 'I1.P2' is inaccessible due to its protection level
                //         I1.P2 -= null;
                Diagnostic(ErrorCode.ERR_BadAccess, "P2").WithArguments("I1.P2").WithLocation(9, 12)
                );
        }
    }
}
