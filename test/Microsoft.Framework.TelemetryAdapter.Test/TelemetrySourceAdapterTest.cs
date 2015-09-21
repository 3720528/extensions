﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Framework.TelemetryAdapter
{
    public class TelemetrySourceAdapterTest
    {
        public class OneTarget
        {
            public int OneCallCount { get; private set; }

            [TelemetryName("One")]
            public void One()
            {
                ++OneCallCount;
            }
        }

        [Fact]
        public void IsEnabled_TrueForEnlistedEvent()
        {
            // Arrange
            var adapter = CreateAdapter(new OneTarget());

            // Act & Assert
            Assert.True(adapter.IsEnabled("One"));
        }

        [Fact]
        public void IsEnabled_FalseForNonenlistedEvent()
        {
            // Arrange
            var adapter = CreateAdapter(new OneTarget());

            // Act & Assert
            Assert.False(adapter.IsEnabled("Two"));
        }

        [Fact]
        public void CallingWriteTelemetryWillInvokeMethod()
        {
            // Arrange
            var target = new OneTarget();
            var adapter = CreateAdapter(target);

            // Act & Assert
            Assert.Equal(0, target.OneCallCount);
            adapter.WriteTelemetry("One", new { });
            Assert.Equal(1, target.OneCallCount);
        }

        [Fact]
        public void CallingWriteTelemetryForNonEnlistedNameIsHarmless()
        {
            // Arrange
            var target = new OneTarget();
            var adapter = CreateAdapter(target);

            // Act & Assert
            Assert.Equal(0, target.OneCallCount);
            adapter.WriteTelemetry("Two", new { });
            Assert.Equal(0, target.OneCallCount);
        }

        private class TwoTarget
        {
            public string Alpha { get; private set; }
            public string Beta { get; private set; }
            public int Delta { get; private set; }

            [TelemetryName("Two")]
            public void Two(string alpha, string beta, int delta)
            {
                Alpha = alpha;
                Beta = beta;
                Delta = delta;
            }
        }

        [Fact]
        public void ParametersWillSplatFromObjectByName()
        {
            // Arrange
            var target = new TwoTarget();
            var adapter = CreateAdapter(target);

            // Act
            adapter.WriteTelemetry("Two", new { alpha = "ALPHA", beta = "BETA", delta = -1 });

            // Assert
            Assert.Equal("ALPHA", target.Alpha);
            Assert.Equal("BETA", target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void ExtraParametersAreHarmless()
        {
            // Arrange
            var target = new TwoTarget();
            var adapter = CreateAdapter(target);

            // Act
            adapter.WriteTelemetry("Two", new { alpha = "ALPHA", beta = "BETA", delta = -1, extra = this });

            // Assert
            Assert.Equal("ALPHA", target.Alpha);
            Assert.Equal("BETA", target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void MissingParametersArriveAsNull()
        {
            // Arrange
            var target = new TwoTarget();
            var adapter = CreateAdapter(target);

            // Act
            adapter.WriteTelemetry("Two", new { alpha = "ALPHA", delta = -1 });

            // Assert
            Assert.Equal("ALPHA", target.Alpha);
            Assert.Null(target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void WriteTelemetryCanDuckType()
        {
            // Arrange
            var target = new ThreeTarget();
            var adapter = CreateAdapter(target);

            // Act
            adapter.WriteTelemetry("Three", new
            {
                person = new Person
                {
                    FirstName = "Alpha",
                    Address = new Address
                    {
                        City = "Beta",
                        State = "Gamma",
                        Zip = 98028
                    }
                }
            });

            // Assert
            Assert.Equal("Alpha", target.Person.FirstName);
            Assert.Equal("Beta", target.Person.Address.City);
            Assert.Equal("Gamma", target.Person.Address.State);
            Assert.Equal(98028, target.Person.Address.Zip);
        }

        public class ThreeTarget
        {
            public IPerson Person { get; private set; }

            [TelemetryName("Three")]
            public void Three(IPerson person)
            {
                Person = person;
            }
        }

        public interface IPerson
        {
            string FirstName { get; }
            string LastName { get; }
            IAddress Address { get; }
        }

        public interface IAddress
        {
            string City { get; }
            string State { get; }
            int Zip { get; }
        }

        public class Person
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Address Address { get; set; }
        }

        public class DerivedPerson : Person
        {
            public double CoolnessFactor { get; set; }
        }

        public class Address
        {
            public string City { get; set; }
            public string State { get; set; }
            public int Zip { get; set; }
        }

        public class SomeValueType
        {
            public SomeValueType(int value)
            {
                Value = value;
            }

            public int Value { get; private set; }
        }

        private static TelemetrySourceAdapter CreateAdapter(object target)
        {
            return new TelemetrySourceAdapter(target, new ProxyTelemetrySourceMethodAdapter());
        }
    }
}
