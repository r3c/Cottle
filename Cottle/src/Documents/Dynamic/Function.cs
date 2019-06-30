﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Cottle.Documents.Dynamic
{
    internal class Function : IFunction
    {
        #region Constructors

        public Function(IEnumerable<string> arguments, Command command, Trimmer trimmer)
        {
            var method = new DynamicMethod(string.Empty, typeof(Value),
                new[] { typeof(Storage), typeof(IReadOnlyList<Value>), typeof(IStore), typeof(TextWriter) }, this.GetType());
            var compiler = new Compiler(method.GetILGenerator(), trimmer);

            this.storage = compiler.Compile(arguments, command);
            this.renderer = (Renderer)method.CreateDelegate(typeof(Renderer));
        }

        #endregion

        #region Methods / Static

        public static void Save(Command command, Trimmer trimmer, string assemblyName, string fileName)
        {
#if NETSTANDARD2_0
            var assembly =
 AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule(fileName);
#else
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName),
                AssemblyBuilderAccess.RunAndSave);
            var module = assembly.DefineDynamicModule(assemblyName, fileName);
#endif

            var program = module.DefineType("Program", TypeAttributes.Public);
            var method = program.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(Value),
                new[] { typeof(Storage), typeof(IList<Value>), typeof(IStore), typeof(TextWriter) });

            var compiler = new Compiler(method.GetILGenerator(), trimmer);
            compiler.Compile(Enumerable.Empty<string>(), command);

#if NETSTANDARD2_0
            program.CreateTypeInfo();
#else
            program.CreateType();
            assembly.Save(fileName);
#endif
        }

        #endregion

        #region Attributes

        private readonly Renderer renderer;

        private readonly Storage storage;

        #endregion

        #region Methods / Public

        public int CompareTo(IFunction other)
        {
            return object.ReferenceEquals(this, other) ? 0 : 1;
        }

        public bool Equals(IFunction other)
        {
            return this.CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return obj is IFunction other && this.Equals(other);
        }

        public Value Execute(IReadOnlyList<Value> arguments, IStore store, TextWriter output)
        {
            return this.renderer(this.storage, arguments, store, output);
        }

        public override int GetHashCode()
        {
            return this.renderer.GetHashCode();
        }

        public override string ToString()
        {
            return "<dynamic>";
        }

        #endregion
    }
}