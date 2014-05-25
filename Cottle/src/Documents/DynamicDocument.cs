﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cottle.Documents.Dynamic;
using Cottle.Parsers;
using Cottle.Settings;
using Cottle.Values;

namespace Cottle.Documents
{
	/// <summary>
	/// Dynamic document compiles template using MSIL generation for better
	/// performance. Code generated by JIT compiler can be reclaimed by garbage
	/// collector, but you should use a caching mechanism to avoid re-creating
	/// too many DynamicDocument instances using the same template source.
	/// </summary>
	public sealed class DynamicDocument : AbstractDocument
	{
		#region Attributes

		private readonly Renderer	renderer;

		private readonly string[]	strings;

		private readonly Value[]	values;

		#endregion

		#region Constructors

		public DynamicDocument (TextReader reader, ISetting setting)
		{
			Allocator		allocator;
			DynamicMethod	method;
			IParser			parser;
			Command			root;

			method = new DynamicMethod (string.Empty, typeof (Value), new [] {typeof (string[]), typeof (Value[]), typeof (IScope), typeof (TextWriter)}, this.GetType ());
			parser = new DefaultParser (setting.BlockBegin, setting.BlockContinue, setting.BlockEnd);

			allocator = new Allocator (method.GetILGenerator ());
			root = parser.Parse (reader);

			this.CompileCommand (allocator, setting.Trimmer, root);

			this.renderer = (Renderer)method.CreateDelegate (typeof (Renderer));
			this.strings = allocator.Strings.ToArray ();
			this.values = allocator.Values.ToArray ();
		}

		public DynamicDocument (TextReader reader) :
			this (reader, DefaultSetting.Instance)
		{
		}

		public DynamicDocument (string template, ISetting setting) :
			this (new StringReader (template), setting)
		{
		}

		public DynamicDocument (string template) :
			this (new StringReader (template), DefaultSetting.Instance)
		{
		}

		#endregion

		#region Methods / Public

		public override Value Render (IScope scope, TextWriter writer)
		{
			return this.renderer (this.strings, this.values, scope, writer);
		}

		#endregion

		#region Methods / Private

		private void CompileCommand (Allocator allocator, Trimmer trimmer, Command command)
		{
			Label	end;

			end = allocator.Generator.DefineLabel ();

			switch (command.Type)
			{
				case CommandType.AssignFunction:
					// FIXME

					break;

				case CommandType.AssignValue:
					// FIXME

					break;

				case CommandType.Composite:
					this.CompileCommand (allocator, trimmer, command.Body);
					this.CompileCommand (allocator, trimmer, command.Next);

					break;

				case CommandType.Dump:
					allocator.Generator.Emit (OpCodes.Ldarg_3);

					this.CompileExpression (allocator, command.Source);
					this.EmitCallWriteObject (allocator);

					break;

				case CommandType.Echo:
					allocator.Generator.Emit (OpCodes.Ldarg_3);

					this.CompileExpression (allocator, command.Source);
					this.EmitCallValueAsString (allocator);
					this.EmitCallWriteString (allocator);

					break;

				case CommandType.For:
					// FIXME

					break;

				case CommandType.If:
					// FIXME

					break;

				case CommandType.Literal:
					allocator.Generator.Emit (OpCodes.Ldarg_3);

					this.EmitString (allocator, trimmer (command.Text));
					this.EmitCallWriteString (allocator);

					break;

				case CommandType.Return:
					this.CompileExpression (allocator, command.Source);

					allocator.Generator.Emit (OpCodes.Br, end);

					break;

				case CommandType.While:
					// FIXME

					break;
			}

			this.EmitVoid (allocator);

			allocator.Generator.MarkLabel (end);
			allocator.Generator.Emit (OpCodes.Ret);
		}

		private void CompileExpression (Allocator allocator, Expression expression)
		{
			ConstructorInfo	constructor;
			LocalBuilder	localPairs;
			Label			success;

			switch (expression.Type)
			{
				case ExpressionType.Access:
					success = allocator.Generator.DefineLabel ();

					this.CompileExpression (allocator, expression.Source);

					allocator.Generator.Emit (OpCodes.Callvirt, Resolver.Property<Func<Value, IMap>> ((value) => value.Fields).GetGetMethod ());

					this.CompileExpression (allocator, expression.Subscript);

					allocator.Generator.Emit (OpCodes.Ldloca_S, allocator.LocalValue);
					allocator.Generator.Emit (OpCodes.Callvirt, typeof (IMap).GetMethod ("TryGet"));
					allocator.Generator.Emit (OpCodes.Brtrue, success);

					this.EmitVoid (allocator);

					allocator.Generator.Emit (OpCodes.Stloc_S, allocator.LocalValue);

					allocator.Generator.MarkLabel (success);
					allocator.Generator.Emit (OpCodes.Ldloc_S, allocator.LocalValue);

					break;

				case ExpressionType.Constant:
					this.EmitValue (allocator, expression.Value);

					break;

				case ExpressionType.Invoke:
					// FIXME

					break;

				case ExpressionType.Map:
					localPairs = allocator.Generator.DeclareLocal (typeof (KeyValuePair<Value, Value>[]));

					allocator.Generator.Emit (OpCodes.Ldc_I4, expression.Elements.Length);
					allocator.Generator.Emit (OpCodes.Newarr, typeof (KeyValuePair<Value, Value>));
					allocator.Generator.Emit (OpCodes.Stloc, localPairs);

					constructor = Resolver.Constructor<Func<Value, Value, KeyValuePair<Value, Value>>> ((key, value) => new KeyValuePair<Value, Value> (key, value));

					for (int i = 0; i < expression.Elements.Length; ++i)
					{
						allocator.Generator.Emit (OpCodes.Ldloc, localPairs);
						allocator.Generator.Emit (OpCodes.Ldc_I4, i);
						allocator.Generator.Emit (OpCodes.Ldelema, typeof (KeyValuePair<Value, Value>));

						this.CompileExpression (allocator, expression.Elements[i].Key);
						this.CompileExpression (allocator, expression.Elements[i].Value);

						allocator.Generator.Emit (OpCodes.Newobj, constructor);
						allocator.Generator.Emit (OpCodes.Stobj, typeof (KeyValuePair<Value, Value>));
					}

					constructor = Resolver.Constructor<Func<IEnumerable<KeyValuePair<Value, Value>>, Value>> ((pairs) => new MapValue (pairs));

					allocator.Generator.Emit (OpCodes.Ldloc, localPairs);
					allocator.Generator.Emit (OpCodes.Newobj, constructor);

					break;

				case ExpressionType.Symbol:
					success = allocator.Generator.DefineLabel ();

					allocator.Generator.Emit (OpCodes.Ldarg_2);

					this.EmitValue (allocator, expression.Value);

					allocator.Generator.Emit (OpCodes.Ldloca_S, allocator.LocalValue);
					allocator.Generator.Emit (OpCodes.Callvirt, typeof (IScope).GetMethod ("Get"));
					allocator.Generator.Emit (OpCodes.Brtrue, success);

					this.EmitVoid (allocator);

					allocator.Generator.Emit (OpCodes.Stloc_S, allocator.LocalValue);

					allocator.Generator.MarkLabel (success);
					allocator.Generator.Emit (OpCodes.Ldloc_S, allocator.LocalValue);

					break;

				case ExpressionType.Void:
					this.EmitVoid (allocator);

					break;
			}
		}

		private void EmitCallValueAsString (Allocator allocator)
		{
			allocator.Generator.Emit (OpCodes.Callvirt, Resolver.Property<Func<Value, string>> ((value) => value.AsString).GetGetMethod ());
		}

		private void EmitCallWriteObject (Allocator allocator)
		{
			allocator.Generator.Emit (OpCodes.Callvirt, Resolver.Method<Action<TextWriter, object>> ((writer, value) => writer.Write (value)));
		}

		private void EmitCallWriteString (Allocator allocator)
		{
			allocator.Generator.Emit (OpCodes.Callvirt, Resolver.Method<Action<TextWriter, string>> ((writer, value) => writer.Write (value)));
		}

		private void EmitString (Allocator allocator, string literal)
		{
			allocator.Generator.Emit (OpCodes.Ldarg_0);
			allocator.Generator.Emit (OpCodes.Ldc_I4, allocator.Allocate (literal));
			allocator.Generator.Emit (OpCodes.Ldelem_Ref);
		}

		private void EmitValue (Allocator allocator, Value constant)
		{
			allocator.Generator.Emit (OpCodes.Ldarg_1);
			allocator.Generator.Emit (OpCodes.Ldc_I4, allocator.Allocate (constant));
			allocator.Generator.Emit (OpCodes.Ldelem_Ref);
		}

		private void EmitVoid (Allocator allocator)
		{
			allocator.Generator.Emit (OpCodes.Call, Resolver.Property<Func<Value>> (() => VoidValue.Instance).GetGetMethod ());
		}

		#endregion
	}
}
