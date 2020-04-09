using System;
using System.Web.Mvc;
using CmsData;
using SharedTestFixtures;
using Xunit;
using Shouldly;
using CmsWeb.Areas.Search.Controllers;
using CmsWeb.Membership;
using CMSWebTests.Support;
using FluentAssertions;

namespace CMSWebTests.Areas.Search.Controllers
{
    [Collection(Collections.Database)]
    public class QueryControllerTests : ControllerTestBase
    {
        private QueryController _controller;

        [Theory]
        [InlineData ("")]
        [InlineData ("simpletagname")]
        public void Should_Add_Peopple_To_Tag(string tagname)
        {
            string TagName = Util2.GetValidTagName(tagname);
            TagName.ShouldNotBeNullOrEmpty();
        }

        private void Setup()
        {
            var username = RandomString();
            var password = RandomString();

            var requestManager = FakeRequestManager.Create();

            requestManager.CurrentHttpContext.Request.Headers["Authorization"] = BasicAuthenticationString(username, password);

            _controller = new QueryController(requestManager);
        }


        [Theory]
        [InlineData("{35cd389a-0c4e-44c3-aac6-98da79c47250}")]
        public void QueryById(Guid id)
        {
            Setup();

            var result = _controller.Index(id) as RedirectResult;

            result.Url.Should().Be("/Query");
        }

        [Theory]
        [InlineData("{35cd389a-0c4e-44c3-aac6-98da79c47250}")]
        public void QueryExportById(Guid id)
        {
            Setup();

            var result = _controller.Export(id) as ContentResult;

            Assert.NotNull(result);
            Assert.Equal("ERROR! This condition does not exist.", result.Content);
        }
    }
}
