﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.Routing;
using Xunit;

namespace AttributeAuthorization.Tests
{
    public class RoutePermissionsBuilderTests
    {
        private const string TemplateName = "routeTemplate";
        private HttpConfiguration _configuration;
        private Dictionary<string, AuthPermissions> _permissions;

        public RoutePermissionsBuilderTests()
        {
            _configuration = new HttpConfiguration();
        }

        private void BuildPermissions(string method, Type restrictTo = null,
            Action<IHttpRoute, Dictionary<string, AuthPermissions>> undefinedRouteAction = null)
        {
            restrictTo = restrictTo ?? typeof(TestController);
            _configuration.Services.Replace(typeof(IHttpControllerTypeResolver), new DefaultHttpControllerTypeResolver(t => t == restrictTo));
            _configuration.Routes.Add("test", new HttpRoute(TemplateName, 
                new HttpRouteValueDictionary 
                {
                    { "controller", restrictTo.Name.Replace("Controller", "") },
                    { "action", method}
                }));

            var builder = new RoutePermissionsBuilder(_configuration, undefinedRouteAction);
            _permissions = builder.Build();
        }

        [Fact]
        public void RequiresNoAuthAttribute_Sets_RequiresNoAuth()
        {
            BuildPermissions("GetPublic");

            var auth = GetPermission();

            Assert.NotNull(auth);
            Assert.True(auth.AuthNotRequired);
            Assert.Empty(auth.Accepted);
        }

        private AuthPermissions GetPermission()
        {
            AuthPermissions result = null;
            _permissions.TryGetValue(TemplateName, out result);

            return result;
        }

        [Fact]
        public void RequiresNoAuth_WithPermissions()
        {
            BuildPermissions("GetPublicWithPermission");

            var auth = GetPermission();

            Assert.NotNull(auth);
            Assert.True(auth.AuthNotRequired);
            Assert.Equal(new List<string> { "permission"}, auth.Accepted);    
        }

        [Fact]
        public void WithMultiplePermissions()
        {
            BuildPermissions("GetMultiple");

            var auth = GetPermission();

            Assert.NotNull(auth);
            Assert.False(auth.AuthNotRequired);
            var accepted = auth.Accepted;
            accepted.Sort();
            Assert.Equal(new List<string> { "permission1", "permission2" }, accepted);
        }

        [Fact]
        public void When_Class_RequiresNoAuth_Sets_RequiresNoAuth()
        {
            BuildPermissions("GetPublic", typeof(Test2Controller));

            var auth = GetPermission();

            Assert.NotNull(auth);
            Assert.True(auth.AuthNotRequired);
            Assert.Empty(auth.Accepted);
        }

        [Fact]
        public void When_Class_RequiresAuth_Method_Inherits()
        {
            BuildPermissions("GetPermission", typeof(Test3Controller));

            var auth = GetPermission();

            Assert.NotNull(auth);
            Assert.False(auth.AuthNotRequired);
            var accepted = auth.Accepted;
            accepted.Sort();
            Assert.Equal(new List<string> { "permission1", "permission2" }, accepted);
        }
    }

    public class TestController : ApiController
    {
        [RequiresNoAuth]
        public string GetPublic()
        {
            return "GetPublic";
        }

        [RequiresNoAuth]
        [RequiresAuth("permission")]
        public string GetPublicWithPermission()
        {
            return "GetPublicWithPermission";
        }

        [RequiresAuth("permission1")]
        [RequiresAuth("permission2")]
        public string GetMultiple()
        {
            return "GetMultiple";
        }
    }

    [RequiresNoAuth]
    public class Test2Controller : ApiController
    {
        public string GetPublic()
        {
            return "Should have RequiresNoAuth";
        }

        [RequiresAuth("permission")]
        public string GetPermission()
        {
            return "GetPermission";
        }
    }

    [RequiresAuth("permission1")]
    public class Test3Controller : ApiController
    {
        [RequiresAuth("permission2")]
        public string GetPermission()
        {
            return "GetPermission";
        }
    }
}
