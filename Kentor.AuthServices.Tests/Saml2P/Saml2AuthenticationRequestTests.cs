﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Xml.Linq;
using System.IdentityModel.Tokens;
using System.Xml;
using Kentor.AuthServices.Saml2P;
using System.Linq;

namespace Kentor.AuthServices.Tests.Saml2P
{
    [TestClass]
    public class Saml2AuthenticationRequestTests
    {
        [TestMethod]
        public void Saml2AuthenticationRequest_ToXElement_RootNode()
        {
            var subject = new Saml2AuthenticationRequest().ToXElement();

            subject.Should().NotBeNull().And.Subject.Name.Should().Be(
                Saml2Namespaces.Saml2P + "AuthnRequest");
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_ToXElement_AddsRequestBaseFields()
        {
            // Just checking for the id field and assuming that means that the
            // base fields are added. The details of the fields are tested
            // by Saml2RequestBaseTests.

            var subject = new Saml2AuthenticationRequest().ToXElement();

            subject.Should().NotBeNull().And.Subject.Attribute("ID").Should().NotBeNull();
            subject.Attribute("AttributeConsumingServiceIndex").Should().BeNull();
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_ToXElement_AddsAttributeConsumingServiceIndex()
        {
            var subject = new Saml2AuthenticationRequest()
            {
                AttributeConsumingServiceIndex = 17
            }.ToXElement();

            subject.Attribute("AttributeConsumingServiceIndex").Value.Should().Be("17");
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_AssertionConsumerServiceUrl()
        {
            string url = "http://some.example.com/Saml2AuthenticationModule/acs";
            var subject = new Saml2AuthenticationRequest()
            {
                AssertionConsumerServiceUrl = new Uri(url)
            }.ToXElement();

            subject.Should().NotBeNull().And.Subject.Attribute("AssertionConsumerServiceURL")
                .Should().NotBeNull().And.Subject.Value.Should().Be(url);
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_Read()
        {
            var xmlData = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<samlp:AuthnRequest
  xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol""
  xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion""
  ID=""Saml2AuthenticationRequest_AssertionConsumerServiceUrl""
  Version=""2.0""
  Destination=""http://destination.example.com""
  AssertionConsumerServiceURL=""https://sp.example.com/SAML2/Acs""
  IssueInstant=""2004-12-05T09:21:59Z"">
  <saml:Issuer>https://sp.example.com/SAML2</saml:Issuer>
/>
</samlp:AuthnRequest>
";

            var relayState = "My relay state";

            var subject = Saml2AuthenticationRequest.Read(xmlData, relayState);

            subject.Id.Should().Be(new Saml2Id("Saml2AuthenticationRequest_AssertionConsumerServiceUrl"));
            subject.AssertionConsumerServiceUrl.Should().Be(new Uri("https://sp.example.com/SAML2/Acs"));
            subject.RelayState.Should().Be(relayState);
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_Read_NoACS()
        {
            var xmlData = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<samlp:AuthnRequest
  xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol""
  xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion""
  ID=""Saml2AuthenticationRequest_Read_NoACS""
  Version=""2.0""
  Destination=""http://destination.example.com""
  IssueInstant=""2004-12-05T09:21:59Z"">
  <saml:Issuer>https://sp.example.com/SAML2</saml:Issuer>
/>
</samlp:AuthnRequest>
";

            var subject = Saml2AuthenticationRequest.Read(xmlData, null);

            subject.Id.Should().Be(new Saml2Id("Saml2AuthenticationRequest_Read_NoACS"));
            subject.AssertionConsumerServiceUrl.Should().Be(null);
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_Read_ShouldThrowOnInvalidVersion()
        {
            var xmlData = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<samlp:AuthnRequest
  xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol""
  xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion""
  ID=""Saml2AuthenticationRequest_Read_ShouldThrowOnInvalidVersion""
  Version=""123456789.0""
  Destination=""http://destination.example.com""
  AssertionConsumerServiceURL=""https://sp.example.com/SAML2/Acs""
  IssueInstant=""2004-12-05T09:21:59Z""
  InResponseTo=""111222333"">
  <saml:Issuer>https://sp.example.com/SAML2</saml:Issuer>
/>
</samlp:AuthnRequest>
";

            Action a = () => Saml2AuthenticationRequest.Read(xmlData, null);

            a.ShouldThrow<XmlException>().WithMessage("Wrong or unsupported SAML2 version");
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_Read_ShouldThrowOnInvalidMessageName()
        {
            var xmlData = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<samlp:NotAuthnRequest
  xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol""
  xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion""
  ID=""Saml2AuthenticationRequest_Read_ShouldThrowOnInvalidMessageName""
  Version=""2.0""
  Destination=""http://destination.example.com""
  AssertionConsumerServiceURL=""https://sp.example.com/SAML2/Acs""
  IssueInstant=""2004-12-05T09:21:59Z""
  InResponseTo=""111222333"">
  <saml:Issuer>https://sp.example.com/SAML2</saml:Issuer>
/>
</samlp:NotAuthnRequest>
";

            Action a = () => Saml2AuthenticationRequest.Read(xmlData, null);

            a.ShouldThrow<XmlException>().WithMessage("Expected a SAML2 authentication request document");
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_Read_NameIdPolicy()
        {
            var xmlData = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<saml2p:AuthnRequest xmlns:saml2p=""urn:oasis:names:tc:SAML:2.0:protocol""
                     xmlns:saml2 =""urn:oasis:names:tc:SAML:2.0:assertion""
                     ID=""ide3c2f1c88255463ab4eb1b158fa6f616""
                     Version=""2.0""
                     IssueInstant=""2016-01-25T13:01:09Z""
                     Destination=""http://destination.example.com""
                     AssertionConsumerServiceURL=""https://sp.example.com/SAML2/Acs""
                     >
    <saml2:Issuer>https://sp.example.com/SAML2</saml2:Issuer>
    <saml2p:NameIDPolicy AllowCreate = ""false"" Format = ""urn:oasis:names:tc:SAML:2.0:nameid-format:persistent"" />
   </saml2p:AuthnRequest>";

            var subject = Saml2AuthenticationRequest.Read(xmlData, null);
            subject.NameIdPolicy.AllowCreate.Should().Be(false);
            subject.NameIdPolicy.Format.Should().Be(NameIdFormat.Persistent);
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_Read_NoFormat()
        {
            var xmlData = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<saml2p:AuthnRequest xmlns:saml2p=""urn:oasis:names:tc:SAML:2.0:protocol""
                     xmlns:saml2 =""urn:oasis:names:tc:SAML:2.0:assertion""
                     ID=""ide3c2f1c88255463ab4eb1b158fa6f616""
                     Version=""2.0""
                     IssueInstant=""2016-01-25T13:01:09Z""
                     Destination=""http://destination.example.com""
                     AssertionConsumerServiceURL=""https://sp.example.com/SAML2/Acs""
                     >
    <saml2:Issuer>https://sp.example.com/SAML2</saml2:Issuer>
    <saml2p:NameIDPolicy AllowCreate = ""false""/>
   </saml2p:AuthnRequest>";

            var subject = Saml2AuthenticationRequest.Read(xmlData, null);
            subject.NameIdPolicy.AllowCreate.Should().Be(false);
            subject.NameIdPolicy.Format.Should().Be(NameIdFormat.NotConfigured);
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_ToXElement_AddsElementSaml2NameIdPolicy_ForAllowCreate()
        {
            var subject = new Saml2AuthenticationRequest()
            {
                AssertionConsumerServiceUrl = new Uri("http://destination.example.com"),
                NameIdPolicy = new Saml2NameIdPolicy(false, NameIdFormat.NotConfigured)
            }.ToXElement();

            var expected = new XElement(Saml2Namespaces.Saml2P + "root",
                new XAttribute(XNamespace.Xmlns + "saml2p", Saml2Namespaces.Saml2P),
                new XElement(Saml2Namespaces.Saml2P + "NameIDPolicy",
                    new XAttribute("AllowCreate", false)))
                    .Elements().Single();

            subject.Attribute("AttributeConsumingServiceIndex").Should().BeNull();
            subject.Element(Saml2Namespaces.Saml2P + "NameIDPolicy")
                .Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_ToXElement_AddsElementSaml2NameIdPolicy_ForNameIdFormat()
        {
            var subject = new Saml2AuthenticationRequest()
            {
                AssertionConsumerServiceUrl = new Uri("http://destination.example.com"),
                NameIdPolicy = new Saml2NameIdPolicy(null, NameIdFormat.EmailAddress)
            }.ToXElement();

            var expected = new XElement(Saml2Namespaces.Saml2P + "root",
                new XAttribute(XNamespace.Xmlns + "saml2p", Saml2Namespaces.Saml2P),
                new XElement(Saml2Namespaces.Saml2P + "NameIDPolicy",
                    new XAttribute("Format", "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress")))
                    .Elements().Single();

            subject.Element(Saml2Namespaces.Saml2P + "NameIDPolicy")
                .Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_ToXElement_AddsRequestedAuthnContext()
        {
            var classRef = "http://www.kentor.se";
            var subject = new Saml2AuthenticationRequest()
            {
                AssertionConsumerServiceUrl = new Uri("http://destination.example.com"),
                RequestedAuthnContext = new Saml2RequestedAuthnContext(new Uri(classRef), AuthnContextComparisonType.Maximum)
            }.ToXElement();

            var expected = new XElement(Saml2Namespaces.Saml2P + "root",
                new XAttribute(XNamespace.Xmlns + "saml2p", Saml2Namespaces.Saml2P),
                new XAttribute(XNamespace.Xmlns + "saml2", Saml2Namespaces.Saml2),
                new XElement(Saml2Namespaces.Saml2P + "RequestedAuthnContext",
                    new XAttribute("Comparison", "Maximum"),
                    new XElement(Saml2Namespaces.Saml2 + "AuthnContextClassRef", classRef)))
                    .Elements().Single();

            var actual = subject.Element(Saml2Namespaces.Saml2P + "RequestedAuthnContext");

            actual.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public void Saml2AuthenticateRequest_ToXElement_OmitsRequestedAuthnContext_OnNullClassRef()
        {
            var subject = new Saml2AuthenticationRequest()
            {
                AssertionConsumerServiceUrl = new Uri("http://destination.example.com"),
                RequestedAuthnContext = new Saml2RequestedAuthnContext(null, AuthnContextComparisonType.Exact)
            }.ToXElement();

            subject.Element(Saml2Namespaces.Saml2P + "RequestedAuthnContext").Should().BeNull();
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_ToXElement_NameFormatTransientForbidsAllowCreate()
        {
            var subject = new Saml2AuthenticationRequest()
            {
                AssertionConsumerServiceUrl = new Uri("http://destination.example.com"),
                NameIdPolicy = new Saml2NameIdPolicy(true, NameIdFormat.Transient)
            };

            subject.Invoking(s => s.ToXElement())
                .ShouldThrow<InvalidOperationException>()
                .And.Message.Should().Be("When NameIdPolicy/Format is set to Transient, it is not permitted to specify AllowCreate. Change Format or leave AllowCreate as null.");
        }

        [TestMethod]
        public void Saml2AuthenticationRequest_Read_ShouldReturnNullOnNullXml()
        {
            string xmlData = null;

            var subject = Saml2AuthenticationRequest.Read(xmlData, null);

            subject.Should().BeNull();
        }
    }
}
