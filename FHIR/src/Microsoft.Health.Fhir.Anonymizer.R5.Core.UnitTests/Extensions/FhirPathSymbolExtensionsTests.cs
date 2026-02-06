using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Extensions
{
    public class FhirPathSymbolExtensionsTests
    {
        [Fact]
        public void GivenListOfElementNodes_WhenGetDecendantsByType_AllNodesShouldBeReturned()
        {
            Patient patient = new Patient();
            patient.Active = true;
            patient.Address.Add(new Address() { City = "Test0" });
            patient.Contact.Add(new Patient.ContactComponent() { Address = new Address() { City = "Test1" } });
            Address address = new Address() { City = "Test2" };

            // R5: ExtendedContactDetail has address field
            ExtendedContactDetail extendedContact = new ExtendedContactDetail();
            extendedContact.Address = new Address() { City = "Test3" };

            // contained resource should not be returned.
            Organization organizationInContained = new Organization();
            var contactInContained = new ExtendedContactDetail();
            contactInContained.Address = new Address() { City = "Test4" };
            organizationInContained.Contact.Add(contactInContained);
            patient.Contained.Add(organizationInContained);

            // Verify primitive object
            Date date = new Date();

            var nodes = new ITypedElement[] { patient.ToTypedElement(), address.ToTypedElement(), extendedContact.ToTypedElement(), date.ToTypedElement() }.Select(n => ElementNode.FromElement(n));
            var results = FhirPathSymbolExtensions.NodesByType(nodes, "Address").Select(n => n.Location);

            Assert.Equal(4, results.Count());
            Assert.Contains("Patient.address[0]", results);
            Assert.Contains("Address", results);
            Assert.Contains("ExtendedContactDetail.address", results);
            Assert.Contains("Patient.contact[0].address", results);
        }

        [Fact]
        public void GivenListOfElementNodes_WhenGetDecendantsByName_AllNodesShouldBeReturned()
        {
            Patient patient = new Patient();
            patient.Active = true;
            patient.Address.Add(new Address() { City = "Test0" });
            patient.Contact.Add(new Patient.ContactComponent() { Address = new Address() { City = "Test1" } });
            Address address = new Address() { City = "Test2" };

            // R5: ExtendedContactDetail has address field
            ExtendedContactDetail extendedContact = new ExtendedContactDetail();
            extendedContact.Address = new Address() { City = "Test3" };

            // contained resource should not be returned.
            Organization organizationInContained = new Organization();
            var contactInContained = new ExtendedContactDetail();
            contactInContained.Address = new Address() { City = "Test4" };
            organizationInContained.Contact.Add(contactInContained);
            patient.Contained.Add(organizationInContained);

            // Verify primitive object
            Date date = new Date();

            var nodes = new ITypedElement[] { patient.ToTypedElement(), address.ToTypedElement(), extendedContact.ToTypedElement(), date.ToTypedElement() }.Select(n => ElementNode.FromElement(n));
            var results = FhirPathSymbolExtensions.NodesByName(nodes, "address").Select(n => n.Location);

            Assert.Equal(4, results.Count());
            Assert.Contains("Patient.address[0]", results);
            Assert.Contains("Address", results);
            Assert.Contains("ExtendedContactDetail.address", results);
            Assert.Contains("Patient.contact[0].address", results);
        }
    }
}