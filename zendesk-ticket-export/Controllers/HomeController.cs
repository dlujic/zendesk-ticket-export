using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.AspNetCore.Mvc;
using zendesk_ticket_export.Models;
using zendesk_ticket_export.ZendeskAPI;

namespace zendesk_ticket_export.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = new ExportTicketsViewModel();
            return View(model);
        }

        [HttpPost]
        public IActionResult ExportTickets(ExportTicketsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var token = "8189629CE22EBAC127857ABFC874D";
                if (model.AccessToken != token)
                {
                    ModelState.AddModelError("unathorized", "Please provide a valid access token to use the export tool.");
                    return View("Index", model);
                }

                var zendesk = new ZendeskClient(model.ZendeskURL, model.Email, model.Password);
                var tickets = zendesk.getAllTicketsIncremental();

                //HttpContext.Request.Headers.Remove("If-Modified-Since");
                //var ticketsDynamic = ConvertTicketsToDynamic(tickets);
                using (var stream = new MemoryStream())
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer))
                {

                    //write headers
                    csv.WriteHeader<Ticket>();

                    var cfs = tickets.First().custom_fields;
                    var cfNames = zendesk.fetchTicketFields();

                    foreach (var cf in cfs)
                    {
                        csv.WriteField(cfNames[cf.id.GetValueOrDefault()]);
                        
                    }

                    csv.NextRecord();
                    //csv.WriteRecords(tickets);
                    //write records

                    foreach (var ticket in tickets)
                    {
                        csv.WriteRecord(ticket);
                        foreach (var cf in ticket.custom_fields)
                        {
                            csv.WriteField(cf.value);
                        }
                        csv.NextRecord();
                    }

                    writer.Flush();
                    return File(stream.ToArray(), "text/csv", BuildCSVFileName());
                }
            }

            return View("Index", model);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string BuildCSVFileName()
        {
            var now = DateTime.Now;
            return "ticket_export_" + now.ToString("yyyy-MM-dd") + ".csv";
        }

        private dynamic ToDynamic(object value)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
            {
                expando.Add(property.Name, property.GetValue(value));
            }

            return expando as ExpandoObject;
        }

        private List<ExpandoObject> ConvertTicketsToDynamic(List<Ticket> tickets)
        {
            var converted = new List<ExpandoObject>();

            foreach (var ticket in tickets)
            {
                var expando = ToDynamic(ticket);
                

                converted.Add(expando);
            }

            return converted;
        }

        private long convertLong(long? num)
        {
            return num ?? default(long);
        }
    }
}

    public class TicketMap : ClassMap<Ticket>
    {
        public TicketMap()
        {
            this.AutoMap();
        }
        public TicketMap(List<CustomField> customFields)
        {
            this.AutoMap();

            foreach (var customField in customFields)
            {
                this.Map(m => m.custom_fields.Where(c => c.id == customField.id).First(), false).Name(customField.id.ToString());
            }
        }
    }

    public class CustomFieldMap : ClassMap<CustomField>
    {
        public CustomFieldMap()
        {
            Map(m => m.value);
            Map(m => m.id).Ignore();
        }
    }

   
