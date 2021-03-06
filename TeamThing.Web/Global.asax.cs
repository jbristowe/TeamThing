﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TeamThing.Web.Core;

namespace TeamThing.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("mobile/{*pathInfo}");

            //routes.MapHttpRoute(
            //    name: "TeamThings",
            //    routeTemplate: "api/team/{id}/things",
            //    defaults: new { controller = "Team", action = "GetThings" }
            //);

            routes.MapHttpRoute(
               name: "PutApi",
               routeTemplate: "api/{controller}/{id}/{action}",
               defaults: new { Action = "Put" },
               constraints: new { httpMethod = new HttpMethodConstraint(new string[] { "PUT" }) }
           );
            routes.MapHttpRoute(
                name: "SingleResourceApi",
                routeTemplate: "api/{controller}/{id}/{action}",
                defaults: new {  },
                constraints: new { id = @"\d+" }
            ); 
            
           

            routes.MapHttpRoute(
                name: "HeaderBasedApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { },
                constraints: new { id = @"\d+" }
            );

            routes.MapHttpRoute(
                name: "ResourceApi",
                routeTemplate: "api/{controller}/{action}"
            );

            routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}"
            );


            ////routes.MapHttpRoute(
            ////    name: "TeamThings",
            ////    routeTemplate: "api/team/{id}/things",
            ////    defaults: new { controller = "Team", action = "GetThings" }
            ////);
            //routes.MapHttpRoute(
            //    name: "SingleResourceApi",
            //    routeTemplate: "api/{controller}/{id}/{action}",
            //    defaults: new { },
            //    constraints: new { id = @"\d+" }
            //);

            //routes.MapHttpRoute(
            //    name: "HttpMethodBasedResourceApi",
            //    routeTemplate: "api/{controller}/{id}",
            //    defaults: new { },
            //    constraints: new { id = @"\d+" }
            //);

            //routes.MapHttpRoute(
            //    name: "ResourceApi",
            //    routeTemplate: "api/{controller}/{action}",
            //    defaults: new { action = "get" }
            //);

            ////routes.MapHttpRoute(
            ////    name: "DefaultApi",
            ////    routeTemplate: "api/{controller}/{id}",
            ////    defaults: new { id = RouteParameter.Optional }
            ////);

            routes.MapRoute(
                name: "Default",
                url: "index.html",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            RegisterApis(GlobalConfiguration.Configuration);
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            BundleTable.Bundles.RegisterTemplateBundles();
        }

        public static void RegisterApis(HttpConfiguration config)
        {
            // Add JavaScriptSerializer  formatter instead - add at top to make default
            //config.Formatters.Insert(0, new JavaScriptSerializerFormatter());

            // Add Json.net formatter - add at the top so it fires first!
            // This leaves the old one in place so JsonValue/JsonObject/JsonArray still are handled

            var index = config.Formatters.IndexOf(config.Formatters.JsonFormatter);
            config.Formatters[0] = new JsonNetFormatter();
        }
    }
}