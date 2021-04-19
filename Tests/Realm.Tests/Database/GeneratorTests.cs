// ////////////////////////////////////////////////////////////////////////////
// //
// // Copyright 2021 Realm Inc.
// //
// // Licensed under the Apache License, Version 2.0 (the "License")
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// // http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.
// //
// ////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Realm.Generator;

namespace Realms.Tests.Database
{
    // RealmPropertiesGenerator
    [RealmClass]
    public partial class SimpleObject : RealmObject
    {
        private int intValue;
        private string stringValue;
    }

    // RealmClassGenerator
    public interface ISimplePerson : IRealmObject
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public IList<int> IntList { get; }

    }

    [TestFixture, Preserve(AllMembers = true)]
    public class AAAAAAGeneratorTests : RealmInstanceTest
    {
        [Test]
        public void RealmPropertiesGenerator()
        {
            var intValue = 1;
            var stringValue = "bla";

            var simpleObject = new SimpleObject
            {
                IntValue = intValue,
                StringValue = stringValue,
            };

            Assert.That(simpleObject.IntValue, Is.EqualTo(intValue));
            Assert.That(simpleObject.StringValue, Is.EqualTo(stringValue));

            _realm.Write(() =>
            {
                _realm.Add(simpleObject, update: true);
            });

            Assert.That(simpleObject.IsManaged, Is.True);

            Assert.That(simpleObject.IntValue, Is.EqualTo(intValue));
            Assert.That(simpleObject.StringValue, Is.EqualTo(stringValue));

            var queried = _realm.All<SimpleObject>().First();
            Assert.That(queried.IntValue, Is.EqualTo(simpleObject.IntValue));
            Assert.That(queried.StringValue, Is.EqualTo(simpleObject.StringValue));

            intValue = 5;
            stringValue = "abracadabra";

            _realm.Write(() =>
            {
                simpleObject.IntValue = intValue;
                simpleObject.StringValue = stringValue;
            });

            Assert.That(simpleObject.IntValue, Is.EqualTo(intValue));
            Assert.That(simpleObject.StringValue, Is.EqualTo(stringValue));

            queried = _realm.All<SimpleObject>().First();
            Assert.That(queried.IntValue, Is.EqualTo(simpleObject.IntValue));
            Assert.That(queried.StringValue, Is.EqualTo(simpleObject.StringValue));
        }

        [Test]
        public void RealmClassGenerator()
        {
            var id = 1;
            var name = "Mary";

            var simplePerson = new SimplePerson
            {
                Id = id,
                Name = name,
            };

            Assert.That(simplePerson.Id, Is.EqualTo(id));
            Assert.That(simplePerson.Name, Is.EqualTo(name));

            _realm.Write(() =>
            {
                _realm.Add(simplePerson, update: true);
            });

            Assert.That(simplePerson.IsManaged, Is.True);

            Assert.That(simplePerson.Id, Is.EqualTo(id));
            Assert.That(simplePerson.Name, Is.EqualTo(name));

            var queried = _realm.All<SimplePerson>().First();
            Assert.That(queried.Id, Is.EqualTo(simplePerson.Id));
            Assert.That(queried.Name, Is.EqualTo(simplePerson.Name));

            id = 5;
            name = "Luis";

            _realm.Write(() =>
            {
                simplePerson.Id = id;
                simplePerson.Name = name;
            });

            Assert.That(simplePerson.Id, Is.EqualTo(id));
            Assert.That(simplePerson.Name, Is.EqualTo(name));

            queried = _realm.All<SimplePerson>().First();
            Assert.That(queried.Id, Is.EqualTo(simplePerson.Id));
            Assert.That(queried.Name, Is.EqualTo(simplePerson.Name));
        }
    }
}
