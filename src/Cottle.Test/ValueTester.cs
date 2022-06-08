﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cottle.Maps;
using Moq;
using NUnit.Framework;

namespace Cottle.Test
{
    public static class ValueTester
    {
        [Test]
        public static void EmptyMap()
        {
            Assert.That(Value.EmptyMap.Type, Is.EqualTo(ValueContent.Map));
            Assert.That(Value.EmptyMap.Fields.Count, Is.EqualTo(0));
        }

        [Test]
        public static void EmptyString()
        {
            Assert.That(Value.EmptyString.Type, Is.EqualTo(ValueContent.String));
            Assert.That(Value.EmptyString.AsString, Is.EqualTo(string.Empty));
        }

        [Test]
        public static void Equals_ShouldCompareBooleans()
        {
            Assert.That(Value.False, Is.EqualTo(Value.False));
            Assert.That(Value.False, Is.Not.EqualTo(Value.True));
            Assert.That(Value.True, Is.EqualTo(Value.True));
            Assert.That(Value.False, Is.Not.EqualTo(Value.Zero));
        }

        [Test]
        public static void Equals_ShouldCompareMaps()
        {
            var a = new KeyValuePair<Value, Value>("A", 1);
            var b = new KeyValuePair<Value, Value>("B", 2);
            var c = new KeyValuePair<Value, Value>("C", 3);
            var d = new KeyValuePair<Value, Value>("D", 4);

            Assert.That(Value.FromEnumerable(new[] { a, b, c }), Is.EqualTo(Value.FromEnumerable(new[] { a, b, c })));
            Assert.That(Value.FromEnumerable(new[] { a, b, c }), Is.Not.EqualTo(Value.FromEnumerable(new[] { a, b, d })));
            Assert.That(Value.FromEnumerable(new[] { a, b, c }), Is.Not.EqualTo(Value.False));
        }

        [Test]
        public static void Equals_ShouldCompareNumbers()
        {
            Assert.That(Value.Zero, Is.EqualTo(Value.Zero));
            Assert.That(Value.Zero, Is.Not.EqualTo(Value.FromNumber(1)));
            Assert.That(Value.Zero, Is.Not.EqualTo(Value.EmptyString));
        }

        [Test]
        public static void Equals_ShouldCompareStrings()
        {
            Assert.That(Value.FromString("A"), Is.EqualTo(Value.FromString("A")));
            Assert.That(Value.FromString("A"), Is.Not.EqualTo(Value.FromString("B")));
            Assert.That(Value.FromString("A"), Is.Not.EqualTo(Value.False));
        }

        [Test]
        public static void Equals_ShouldCompareVoid()
        {
            Assert.That(Value.Undefined, Is.EqualTo(Value.Undefined));
            Assert.That(Value.Undefined, Is.Not.EqualTo(Value.False));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public static void FromBoolean(bool input)
        {
            var value = Value.FromBoolean(input);

            Assert.That(value.Type, Is.EqualTo(ValueContent.Boolean));
            Assert.That(value.AsBoolean, Is.EqualTo(input));
        }

        [Test]
        [TestCase('a')]
        [TestCase('©')]
        public static void FromCharacter(char input)
        {
            var value = Value.FromCharacter(input);

            Assert.That(value.Type, Is.EqualTo(ValueContent.String));
            Assert.That(value.AsString[0], Is.EqualTo(input));
        }

        [Test]
        public static void FromDictionary()
        {
            var elements = new Dictionary<Value, Value> { ["A"] = 17, [42] = "B" };
            var value = Value.FromDictionary(elements);

            Assert.That(value.Type, Is.EqualTo(ValueContent.Map));
            Assert.That(new List<KeyValuePair<Value, Value>>(value.Fields), Is.EqualTo(elements));
        }

        [Test]
        public static void FromEnumerable_KeyValue()
        {
            var elements = new[] { new KeyValuePair<Value, Value>("A", 17), new KeyValuePair<Value, Value>(42, "B") };
            var value = Value.FromEnumerable(elements);

            Assert.That(value.Type, Is.EqualTo(ValueContent.Map));
            Assert.That(new List<KeyValuePair<Value, Value>>(value.Fields), Is.EqualTo(elements));
        }

        [Test]
        public static void FromEnumerable_Value()
        {
            var elements = new Value[] { 4, 8, 15, 16, 23, 42 };
            var expected = elements.Select((element, index) => new KeyValuePair<Value, Value>(index, element)).ToList();
            var value = Value.FromEnumerable(elements);

            Assert.That(value.Type, Is.EqualTo(ValueContent.Map));
            Assert.That(new List<KeyValuePair<Value, Value>>(value.Fields), Is.EqualTo(expected));
        }

        [Test]
        public static void FromEvaluable()
        {
            var evaluable = new Mock<IEvaluable>();
            var value = Value.FromEvaluable(evaluable.Object);

            evaluable.Setup(e => e.Type).Returns(ValueContent.Boolean);
            evaluable.Setup(e => e.AsBoolean).Returns(true);

            Assert.That(value.Type, Is.EqualTo(ValueContent.Boolean));
            Assert.That(value.AsBoolean, Is.True);

            evaluable.Setup(e => e.Type).Returns(ValueContent.Number);
            evaluable.Setup(e => e.AsNumber).Returns(17);

            Assert.That(value.Type, Is.EqualTo(ValueContent.Number));
            Assert.That(value.AsNumber, Is.EqualTo(17d));

            evaluable.Setup(e => e.Type).Returns(ValueContent.Function);
            evaluable.Setup(e => e.AsFunction).Returns(Function.CreatePure((s, a) => 42));

            Assert.That(value.Type, Is.EqualTo(ValueContent.Function));
            Assert.That(value.AsFunction.Invoke(new(), Array.Empty<Value>(), TextWriter.Null),
                Is.EqualTo(Value.FromNumber(42)));

            evaluable.Setup(e => e.Type).Returns(ValueContent.String);
            evaluable.Setup(e => e.AsString).Returns("test");

            Assert.That(value.Type, Is.EqualTo(ValueContent.String));
            Assert.That(value.AsString, Is.EqualTo("test"));

            evaluable.Setup(e => e.Type).Returns(ValueContent.Map);
            evaluable.Setup(e => e.Fields).Returns(new ArrayMap(new Value[] { 1, 3, 7 }));

            Assert.That(value.Type, Is.EqualTo(ValueContent.Map));
            Assert.That(value.Fields.Count, Is.EqualTo(3));
            Assert.That(value.Fields[0], Is.EqualTo(Value.FromNumber(1)));
            Assert.That(value.Fields[1], Is.EqualTo(Value.FromNumber(3)));
            Assert.That(value.Fields[2], Is.EqualTo(Value.FromNumber(7)));

            evaluable.Setup(e => e.Type).Returns(ValueContent.Void);

            Assert.That(value.Type, Is.EqualTo(ValueContent.Void));
        }

        [Test]
        public static void FromFunction()
        {
            var expected = Value.FromNumber(59);
            var value = Value.FromFunction(Function.CreatePure((s, a) => expected));

            Assert.That(value.Type, Is.EqualTo(ValueContent.Function));
            Assert.That(value.AsFunction.IsPure, Is.True);
            Assert.That(value.AsFunction.Invoke(new(), Array.Empty<Value>(), TextWriter.Null), Is.EqualTo(expected));
        }

        [Test]
        public static void FromGenerator()
        {
            var value = Value.FromGenerator(i => i * 3, 5);

            Assert.That(value.Type, Is.EqualTo(ValueContent.Map));
            Assert.That(value.Fields.Count, Is.EqualTo(5));
            Assert.That(value.Fields[0], Is.EqualTo(Value.Zero));
            Assert.That(value.Fields[4], Is.EqualTo(Value.FromNumber(12)));
            Assert.That(value.Fields[5], Is.EqualTo(Value.Undefined));
        }

        [Test]
        [TestCaseSource(nameof(Values))]
        public static void FromLazy_ShouldCompare(Value resolved)
        {
            Assert.That(Value.FromLazy(() => resolved), Is.EqualTo(resolved));
            Assert.That(Value.FromLazy(() => Value.FromLazy(() => resolved)), Is.EqualTo(resolved));
        }

        [Test]
        public static void FromLazy_ShouldResolve()
        {
            var resolved = false;
            var resolver = new Func<Value>(() =>
            {
                resolved = true;

                return 13;
            });

            var value = Value.FromLazy(resolver);

            Assert.That(resolved, Is.False);
            Assert.That(value.Type, Is.EqualTo(ValueContent.Number));
            Assert.That(value.AsNumber, Is.EqualTo(13d));
            Assert.That(resolved, Is.True);
        }

        [Test]
        public static void FromMap()
        {
            var expected = Value.FromString("field");
            var index = Value.FromString("index");
            var map = new Mock<IMap>();
            var value = Value.FromMap(map.Object);

            map.Setup(m => m[index]).Returns(expected);

            Assert.That(value.Type, Is.EqualTo(ValueContent.Map));
            Assert.That(value.Fields[index], Is.EqualTo(expected));
        }

        private static readonly IReadOnlyList<TestCaseData> FromNumber_Input = new[]
        {
            new TestCaseData(Value.FromNumber(double.NaN), double.NaN),
            new TestCaseData(Value.FromNumber(0d), 0),
            new TestCaseData(Value.FromNumber(1d), 1),
            new TestCaseData(Value.FromNumber(5.3d), 5.3),
            new TestCaseData(Value.FromNumber((byte)17), 17),
            new TestCaseData(Value.FromNumber(17d), 17),
            new TestCaseData(Value.FromNumber(17f), 17),
            new TestCaseData(Value.FromNumber(17), 17),
            new TestCaseData(Value.FromNumber(17L), 17),
            new TestCaseData(Value.FromNumber((sbyte)17), 17),
            new TestCaseData(Value.FromNumber((short)17), 17),
            new TestCaseData(Value.FromNumber((ushort)17), 17),
            new TestCaseData(Value.FromNumber((uint)17), 17),
            new TestCaseData(Value.FromNumber(17UL), 17)
        };

        [Test]
        [TestCaseSource(nameof(FromNumber_Input))]
        public static void FromNumber(Value value, double expected)
        {
            Assert.That(value.Type, Is.EqualTo(ValueContent.Number));
            Assert.That(value.AsNumber, Is.EqualTo(expected));
        }

        private static readonly IReadOnlyList<TestCaseData> FromReflection_Input = new[]
        {
            new TestCaseData(new ReferenceFieldContainer<bool> { Field = true }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = true })),
            new TestCaseData(new ReferenceFieldContainer<byte> { Field = 4 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = 4 })),
            new TestCaseData(new ReferenceFieldContainer<sbyte> { Field = -5 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = -5 })),
            new TestCaseData(new ReferenceFieldContainer<short> { Field = -9 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = -9 })),
            new TestCaseData(new ReferenceFieldContainer<ushort> { Field = 8 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = 8 })),
            new TestCaseData(new ReferenceFieldContainer<int> { Field = 42 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = 42 })),
            new TestCaseData(new ReferenceFieldContainer<uint> { Field = 42 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = 42U })),
            new TestCaseData(new ReferenceFieldContainer<long> { Field = 17 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = 17L })),
            new TestCaseData(new ReferenceFieldContainer<ulong> { Field = 24 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = 24L })),
            new TestCaseData(new ReferenceFieldContainer<float> { Field = 1 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = 1f })),
            new TestCaseData(new ReferenceFieldContainer<double> { Field = 3 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = 3d })),
            new TestCaseData(new ReferenceFieldContainer<char> { Field = 'x' }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = 'x' })),
            new TestCaseData(new ReferenceFieldContainer<string> { Field = "abc" }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = "abc" })),
            new TestCaseData(new ReferencePropertyContainer<bool> { Property = true }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = true })),
            new TestCaseData(new ReferencePropertyContainer<byte> { Property = 4 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = 4 })),
            new TestCaseData(new ReferencePropertyContainer<sbyte> { Property = -5 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = -5 })),
            new TestCaseData(new ReferencePropertyContainer<short> { Property = -9 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = -9 })),
            new TestCaseData(new ReferencePropertyContainer<ushort> { Property = 8 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = 8 })),
            new TestCaseData(new ReferencePropertyContainer<int> { Property = 42 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = 42 })),
            new TestCaseData(new ReferencePropertyContainer<uint> { Property = 42 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = 42U })),
            new TestCaseData(new ReferencePropertyContainer<long> { Property = 17 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = 17L })),
            new TestCaseData(new ReferencePropertyContainer<ulong> { Property = 24 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = 24L })),
            new TestCaseData(new ReferencePropertyContainer<float> { Property = 1 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = 1f })),
            new TestCaseData(new ReferencePropertyContainer<double> { Property = 3 }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = 3d })),
            new TestCaseData(new ReferencePropertyContainer<char> { Property = 'x' }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = 'x' })),
            new TestCaseData(new ReferencePropertyContainer<string> { Property = "abc" }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = "abc" })),
            new TestCaseData(new ValueFieldContainer<bool> { Field = true }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = true })),
            new TestCaseData(new ValueFieldContainer<string> { Field = "abc" }, Value.FromDictionary(new Dictionary<Value, Value> { ["Field"] = "abc" })),
            new TestCaseData(new ValuePropertyContainer<bool> { Property = true }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = true })),
            new TestCaseData(new ValuePropertyContainer<string> { Property = "abc" }, Value.FromDictionary(new Dictionary<Value, Value> { ["Property"] = "abc" })),
            new TestCaseData(new [] {1, 3, 5}, Value.FromEnumerable(new Value[] {1, 3, 5})),
            new TestCaseData(new [] {"a", "b", "c"}, Value.FromEnumerable(new Value[] {"a", "b", "c"}))
        };

        [Test]
        [TestCaseSource(nameof(FromReflection_Input))]
        public static void FromReflection<T>(T reference, Value expected)
        {
            var value = Value.FromReflection(reference, BindingFlags.Instance | BindingFlags.Public);

            Assert.That(value, Is.EqualTo(expected));
        }

        [Test]
        [TestCase("")]
        [TestCase("Hello!")]
        public static void FromString_NotNull(string input)
        {
            var value = Value.FromString(input);

            Assert.That(value.Type, Is.EqualTo(ValueContent.String));
            Assert.That(value.AsString, Is.EqualTo(input));
        }

        [Test]
        public static void FromString_Null()
        {
            var value = Value.FromString(null);

            Assert.That(value.Type, Is.EqualTo(ValueContent.Void));
        }

        [Test]
        public static void False()
        {
            Assert.That(Value.False.Type, Is.EqualTo(ValueContent.Boolean));
            Assert.That(Value.False.AsBoolean, Is.False);
        }

        [Test]
        public static void True()
        {
            Assert.That(Value.True.Type, Is.EqualTo(ValueContent.Boolean));
            Assert.That(Value.True.AsBoolean, Is.True);
        }

        [Test]
        public static void Undefined()
        {
            Assert.That(Value.Undefined, Is.EqualTo(default(Value)));
            Assert.That(Value.Undefined.Type, Is.EqualTo(ValueContent.Void));
        }

        [Test]
        public static void Zero()
        {
            Assert.That(Value.Zero.Type, Is.EqualTo(ValueContent.Number));
            Assert.That(Value.Zero.AsNumber, Is.EqualTo(0));
        }

        private class ReferenceFieldContainer<T>
        {
            // ReSharper disable once NotAccessedField.Local
            public T Field = default!;
        }

        private struct ValueFieldContainer<T>
        {
            // ReSharper disable once NotAccessedField.Local
            public T Field;
        }

        private class ReferencePropertyContainer<T>
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public T Property { get; set; } = default!;
        }

        private struct ValuePropertyContainer<T>
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public T Property { get; set; }
        }

        private static readonly IReadOnlyList<TestCaseData> Values = new[]
        {
            new TestCaseData(Value.EmptyMap),
            new TestCaseData(Value.EmptyString),
            new TestCaseData(Value.False),
            new TestCaseData(Value.True),
            new TestCaseData(Value.Undefined),
            new TestCaseData(Value.Zero),
            new TestCaseData(Value.FromCharacter('a')),
            new TestCaseData(Value.FromDictionary(new Dictionary<Value, Value> { ["A"] = 1, ["B"] = 2 })),
            new TestCaseData(Value.FromEnumerable(new Value[] {1, "A", true})),
            new TestCaseData(Value.FromNumber(42)),
            new TestCaseData(Value.FromString("abc"))
        };
    }
}