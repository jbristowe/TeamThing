﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using TeamThing.Web.Core.Mappers;
using DomainModel = TeamThing.Model;
using ServiceModel = TeamThing.Web.Models.API;
using System.Net;
using System.Json;
using TeamThing.Web.Core.Helpers;

namespace TeamThing.Web.Controllers
{
    public class ThingController : ApiController
    {
        private readonly DomainModel.TeamThingContext context;

        public ThingController()
        {
            this.context = new DomainModel.TeamThingContext();
        }

        public IQueryable<ServiceModel.Thing> Get()
        {
            //TODO: We chould grab the current user here probably?
            return context.GetAll<DomainModel.Thing>().MapToServiceModel();
        }

        public HttpResponseMessage Get(int id)
        {
            //TODO: We chould grab the current user here probably?
            var thing = context.GetAll<DomainModel.Thing>().FirstOrDefault(t => t.Id == id);

            if (thing == null)
            {
                ModelState.AddModelError("", "Invalid Thing");
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }

            var sThing = thing.MapToServiceModel();
            var response = new HttpResponseMessage<ServiceModel.Thing>(sThing, HttpStatusCode.OK);
            response.Headers.Location = new Uri(Request.RequestUri, "/api/thing/" + thing.Id.ToString());
            return response;
        }
       
        public HttpResponseMessage Post(ServiceModel.AddThingViewModel newThing)
        {
            if (!ModelState.IsValid)
            {
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }

            var thingCreator = context.GetAll<DomainModel.User>()
                                      .FirstOrDefault(u => u.Id == newThing.CreatedById);

            if (thingCreator == null)
            {
                ModelState.AddModelError("", "Invalid Creator");
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }

            var team = context.GetAll<DomainModel.Team>()
                                      .FirstOrDefault(u => u.Id == newThing.TeamId);

            if (team == null)
            {
                ModelState.AddModelError("", "Invalid Team");
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }


            var thing = new DomainModel.Thing(team, thingCreator);
            thing.Description = newThing.Description;

            foreach (var userId in newThing.AssignedTo)
            {
                var assignedTo = context.GetAll<DomainModel.User>()
                                          .FirstOrDefault(u => u.Id == userId);

                if (assignedTo == null)
                {
                    ModelState.AddModelError("", "Invalid User Assigned to Thing");
                    return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
                }

                thing.AssignedTo.Add(new DomainModel.UserThing(thing, assignedTo, thingCreator));
            }

            context.Add(thing);
            context.SaveChanges();

            var sThing = thing.MapToServiceModel();
            var response = new HttpResponseMessage<ServiceModel.Thing>(sThing, HttpStatusCode.Created);
            response.Headers.Location = new Uri(Request.RequestUri, "/api/thing/" + thing.Id.ToString());
            return response;
        }

        [HttpPut]
        public HttpResponseMessage UpdateStatus(int id, ServiceModel.UpdateThingStatusViewModel updateStatusParameters)
        {
            var thing = context.GetAll<DomainModel.Thing>()
                            .FirstOrDefault(u => u.Id == id);

            DomainModel.ThingStatus realStatus;

            if (!Enum.TryParse<DomainModel.ThingStatus>(updateStatusParameters.Status, true, out realStatus))
            {
                ModelState.AddModelError("", "Invalid Status");
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }

            if (thing == null)
            {
                ModelState.AddModelError("", "Invalid Thing");
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }

            if (!thing.AssignedTo.Any(at => at.AssignedToUserId == updateStatusParameters.UserId))
            {
                ModelState.AddModelError("", "A thing can only be removed by its owner.");
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }

            thing.UpdateStatus(updateStatusParameters.UserId, realStatus);
            context.SaveChanges();

            var sThing = thing.MapToServiceModel();
            var response = new HttpResponseMessage<ServiceModel.Thing>(sThing, HttpStatusCode.OK);
            response.Headers.Location = new Uri(Request.RequestUri, "/api/thing/" + thing.Id.ToString());
            return response;
        }

        [HttpPut]
        public HttpResponseMessage Complete(int id, ServiceModel.CompleteThingViewModel viewModel)
        {

            var thing = context.GetAll<DomainModel.Thing>()
                               .FirstOrDefault(u => u.Id == id);

            if (thing == null)
            {
                ModelState.AddModelError("", "Invalid Thing");
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }

            if (!thing.AssignedTo.Any(at => at.AssignedToUserId == viewModel.UserId))
            {
                ModelState.AddModelError("", "A thing can only be removed by its owner.");
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }

            thing.Complete(viewModel.UserId);
            context.SaveChanges();

            var sThing = thing.MapToServiceModel();
            var response = new HttpResponseMessage<ServiceModel.Thing>(sThing, HttpStatusCode.OK);
            response.Headers.Location = new Uri(Request.RequestUri, "/api/thing/" + thing.Id.ToString());
            return response;
        }

        [HttpPut]
        public HttpResponseMessage Put(int id, ServiceModel.UpdateThingViewModel viewModel)
        {
            var thingEditor = context.GetAll<DomainModel.User>()
                                     .FirstOrDefault(u => u.Id == viewModel.EditedById);

            var thing = context.GetAll<DomainModel.Thing>()
                               .FirstOrDefault(u => u.Id == id);

            if (thingEditor == null)
            {
                ModelState.AddModelError("", "Invalid Editor");
            }
            if (thing == null)
            {
                ModelState.AddModelError("", "Invalid Thing");
            }
            if (thing != null && thing.OwnerId != thingEditor.Id)
            {
                ModelState.AddModelError("", "A Thing can only be edited by its owner");
            }

            if (!ModelState.IsValid)
            {
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }

            foreach (var userId in viewModel.AssignedTo)
            {
                //already assigned
                if (thing.AssignedTo.Any(at => at.AssignedToUserId == userId)) continue;


                var assignedTo = context.GetAll<DomainModel.User>()
                                          .FirstOrDefault(u => u.Id == userId);

                if (assignedTo == null)
                {
                    throw new HttpResponseException("Invalid User Assigned to Thing", HttpStatusCode.NotFound);
                }

                thing.AssignedTo.Add(new DomainModel.UserThing(thing, assignedTo, thingEditor));
            }

            //removed users
            var removedUserIds = thing.AssignedTo.Select(at => at.AssignedToUserId).Except(viewModel.AssignedTo);
            var removedUserThings = thing.AssignedTo.Where(at => removedUserIds.Contains(at.AssignedToUserId)).ToList();

            context.Delete(removedUserThings);

            thing.Description = viewModel.Description;

            context.SaveChanges();

            var sThing = thing.MapToServiceModel();
            var response = new HttpResponseMessage<ServiceModel.Thing>(sThing, HttpStatusCode.OK);
            response.Headers.Location = new Uri(Request.RequestUri, "/api/thing/" + thing.Id.ToString());
            return response;
        }

        [HttpDelete]
        public HttpResponseMessage Delete(int id, ServiceModel.DeleteThingViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return new HttpResponseMessage<JsonValue>(ModelState.ToJson(), HttpStatusCode.BadRequest);
            }

            var thing = context.GetAll<DomainModel.Thing>()
                               .FirstOrDefault(u => u.Id == id);

            //rest spec says we should not throw an error in this case ( delete requests should be idempotent)
            if (thing == null)
            {
                throw new HttpResponseException("Invalid Thing", HttpStatusCode.BadRequest);
            }

            if (thing.OwnerId != viewModel.DeletedById)
            {
                throw new HttpResponseException("A thing can only be removed by its owner.", HttpStatusCode.BadRequest);
            }

            thing.Delete(viewModel.DeletedById);
            context.SaveChanges();

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }  
}
