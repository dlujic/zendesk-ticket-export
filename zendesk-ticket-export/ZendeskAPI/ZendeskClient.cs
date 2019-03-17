using NLog;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace zendesk_ticket_export.ZendeskAPI
{
    public class ZendeskClient
    {
        public RestClient Client { get; set; }

        public ZendeskClient(String url, String username, String password)
        {
            URL = url;
            Username = username;
            Password = password;
            logger.Trace("Building Zendesk Client constructor.");
            Client = new RestClient(url);
            Client.Authenticator = new HttpBasicAuthenticator(username, password);
            logger.Trace("End Zendesk Client constructor.");
        }

        public Dictionary<long, string> fetchTicketFields()
        {
            var customFieldsRequest = new RestRequest();
            var customFieldsString = apiPreface + "ticket_fields.json";
            customFieldsRequest.Resource = customFieldsString;

            var customFieldsJSONResponse = Client.Execute<CustomFieldDefinitionJSONOutput>(customFieldsRequest);
            while ((int)customFieldsJSONResponse.StatusCode == 429)
            {
                var retryAfter = customFieldsJSONResponse.Headers.First(x => x.Name == "Retry-After").Value;
                var milisecondsToWait = (int)retryAfter * 1000;
                logger.Error("Rate limit reached. Retry-After: " + (int)retryAfter);
                System.Threading.Thread.Sleep(milisecondsToWait);
                customFieldsJSONResponse = Client.Execute<CustomFieldDefinitionJSONOutput>(customFieldsRequest);
            }

            return customFieldsJSONResponse.Data.ticket_fields.ToDictionary(key => key.id, val => val.title);
        }

        public void updateManyUsers(string jsonBody = null)
        {
            logger.Trace("*******");
            logger.Trace("Starting update many users procedure.");
            logger.Info("Preparing update request.");

            var updateUsersRequest = new RestRequest();
            string updateUsersResource = apiPreface + "users/update_many.json";
            updateUsersRequest.Resource = updateUsersResource;
            updateUsersRequest.Method = Method.PUT;
            //updateUsersRequest.AddJsonBody(userList);

            logger.Debug(jsonBody);
            updateUsersRequest.AddParameter("application/json", jsonBody, ParameterType.RequestBody);

            var updateResponse = Client.Execute<JobStatusJSONResponse>(updateUsersRequest);

            if (updateResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                logger.Info("HTTP status OK.");
                logger.Trace("Job queued.");
                logger.Fatal("Please note that the job is now queued and the update process should complete shortly. This application will continue checking the job status every 5 seconds and close when complete.");

            }

            else
            {
                logger.Warn("HTTP status {0}.", updateResponse.StatusCode.ToString());
                logger.Warn("JOB NOT QUEUED.");
            }


            try
            {
                checkJobStatus(updateResponse.Data.job_status.id);
            }
            catch (NullReferenceException e)
            {
                logger.Fatal("Couldn't print all results information due to exception.");
                logger.Warn("Exception message: {0}.", e.Message);
                logger.Warn("Program continued despite the printing error.");
            }
            finally
            {
                logger.Trace("End update many users procedure.");
                logger.Trace("********");
            }
        }

        public void checkJobStatus(String id)
        {
            logger.Trace("Starting checkJobStatus procedure.");

            var checkStatusRequest = new RestRequest();
            string checkStatusString = apiPreface + "job_statuses/" + id + ".json";

            checkStatusRequest.Resource = checkStatusString;

            var checkStatusResponse = Client.Execute<JobStatusJSONResponse>(checkStatusRequest);
            var job_status = checkStatusResponse.Data.job_status;

            for (; ; )
            {
                logger.Error("Checking job status.");
                logger.Info("*-* Total: {0}.", job_status.total);
                logger.Info("*-* Progress: {0}.", job_status.progress);
                logger.Info("*-* Job status: {0}.", job_status.status);
                logger.Info("*-* Job message: {0}.", job_status.message);
                logger.Info("*-* Results: [");

                foreach (var result in job_status.results)
                {
                    logger.Info("*-*-{");
                    foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(result))
                    {
                        string name = descriptor.Name;
                        object value = descriptor.GetValue(result);
                        logger.Info("*-*-* {0} : {1}", name, value);
                    }
                    logger.Info("*-*-}");
                }

                if (job_status.status == "completed")
                    break;

                logger.Warn("Waing 5 seconds before next attempt.");
                System.Threading.Thread.Sleep(5000);

                checkStatusResponse = Client.Execute<JobStatusJSONResponse>(checkStatusRequest);
                job_status = checkStatusResponse.Data.job_status;
            }


            logger.Trace("Ending checkJobStatus procedure.");

        }

        public List<Attachment> downloadAllAttachments(String workingDirectory)
        {
            logger.Trace("Starting downloadAllAttachments procedure.");
            logger.Info("Beginning to obtain all tickets");
            var tickets = getAllTicketsIncremental();
            logger.Info("Obtained all tickets.");
            var allAttachments = downloadAttachments(workingDirectory, tickets);
            logger.Trace("End downloadAllAttachments procedure.");
            return allAttachments;

        }

        public List<Attachment> downloadAttachments(String workingDirectory, List<Ticket> tickets)
        {
            logger.Trace("Starting downloadAAttachments procedure.");
            var allAttachments = new List<Attachment>();

            logger.Info("Tickets: {0}.", tickets.Count);
            var ticketCounter = 1;
            foreach (var ticket in tickets)
            {
                logger.Info("Working on ticket #{0}, {1} out of {2}", ticket.id, ticketCounter, tickets.Count);
                ticketCounter++;

                logger.Info("Getting ticket comments.");
                var commentsRequest = new RestRequest();
                var commentsString = apiPreface + "tickets/" + ticket.id + "/comments.json";
                commentsRequest.Resource = commentsString;

                var commentsJSONResponse = Client.Execute<CommentsJSONResponse>(commentsRequest);
                var comments = new List<TicketComment>();

                if ((int)commentsJSONResponse.StatusCode == 429)
                {
                    do
                    {
                        var retryAfter = commentsJSONResponse.Headers.First(x => x.Name == "Retry-After").Value;
                        var milisecondsToWait = Convert.ToInt32(retryAfter) * 1000;
                        logger.Error("Rate limit reached, Retry-After: {0}.", (int)retryAfter);
                        System.Threading.Thread.Sleep(milisecondsToWait);
                        commentsJSONResponse = Client.Execute<CommentsJSONResponse>(commentsRequest);
                    } while ((int)commentsJSONResponse.StatusCode == 429);
                }

                comments.AddRange(commentsJSONResponse.Data.comments);

                var pageNumberComments = 2;
                if (commentsJSONResponse.Data != null)
                {
                    while (commentsJSONResponse.Data.next_page != null)
                    {

                        var nextCommentPageString = apiPreface + "tickets/" + ticket.id + "/comments.json?page=" + pageNumberComments;
                        var nextCommentPageRequest = new RestRequest();
                        nextCommentPageRequest.Resource = nextCommentPageString;
                        commentsJSONResponse = Client.Execute<CommentsJSONResponse>(nextCommentPageRequest);
                        if ((int)commentsJSONResponse.StatusCode == 429)
                        {
                            do
                            {
                                var retryAfter = commentsJSONResponse.Headers.First(x => x.Name == "Retry-After").Value;
                                var milisecondsToWait = Convert.ToInt32(retryAfter) * 1000;
                                logger.Error("Rate limit reached, Retry-After: {0}.", (int)retryAfter);
                                System.Threading.Thread.Sleep(milisecondsToWait);
                                commentsJSONResponse = Client.Execute<CommentsJSONResponse>(nextCommentPageRequest);
                            } while ((int)commentsJSONResponse.StatusCode == 429);
                        }
                        if (commentsJSONResponse.Data.comments != null)
                        {
                            if (commentsJSONResponse.Data.comments.Any())
                            {
                                comments.AddRange(commentsJSONResponse.Data.comments);
                            }
                            pageNumberComments++;
                        }
                        else
                        {
                            logger.Fatal("Comments are null!!");
                            throw new Exception("Unexpected comments null value encountered.");
                        }

                    }

                    for (int i = 1; i <= comments.Count; i++)
                    {
                        comments[i - 1].CommentNumber = i;
                    }
                    logger.Info("Finished obtaining ticket comments.");
                    logger.Info("Checking for any attachments...");

                    if (comments != null && comments.Any())
                    {
                        foreach (var comment in comments)
                        {
                            if (comment.attachments != null && comment.attachments.Any())
                            {
                                int attachmentCounter = 1;
                                logger.Warn("{0} Attachment(s) found for comment {1}.", comment.attachments.Count, comment.CommentNumber);
                                foreach (var attachment in comment.attachments)
                                {
                                    logger.Info("Working on attachment {0} of {1}", attachmentCounter, comment.attachments.Count);
                                    attachment.TicketID = ticket.id;
                                    attachment.CommentNumber = comment.CommentNumber;
                                    allAttachments.Add(attachment);

                                    var attachmentDirectory = Path.Combine(workingDirectory,
                                                                            "Attachments",
                                                                            ticket.id.ToString(),
                                                                            "comments",
                                                                            comment.CommentNumber.ToString());
                                    if (!File.Exists(Path.Combine(attachmentDirectory, attachment.file_name)))
                                    {
                                        var relative_path = attachment.content_url.Split(new String[] { "zendesk.com/" }, StringSplitOptions.None)[1];
                                        var downloadLink = relative_path;
                                        var downloadRequest = new RestRequest();
                                        downloadRequest.Resource = downloadLink;

                                        var dlClient = new RestClient(Client.BaseUrl);
                                        dlClient.Authenticator = new HttpBasicAuthenticator(Username, Password);
                                        dlClient.ClearHandlers();
                                        var fileBytes = dlClient.DownloadData(downloadRequest);

                                        Directory.CreateDirectory(attachmentDirectory);
                                        File.WriteAllBytes(Path.Combine(attachmentDirectory, attachment.file_name), fileBytes);
                                        logger.Info("Attachment {0} saved.", attachment.file_name);
                                    }
                                    else
                                    {
                                        logger.Error("File already exists! Proceeding.");
                                    }
                                }

                            }
                            else
                            {
                                logger.Info("No attachments found for comment {0}.", comment.CommentNumber);
                            }
                        }
                        logger.Info("Finished checking for attachments for ticket #{0}.", ticket.id);
                    }
                    else
                    {
                        logger.Warn("No comments found for this ticket.");
                    }
                }

            }
            logger.Trace("End downloadAttachments procedure.");
            return allAttachments;

        }

        public List<Ticket> getAllTicketsIncremental()
        {
            logger.Trace("Starting getAllTicketsIncremental procedure.");
            logger.Info("Creating initial request.");
            var ticketsRequest = new RestRequest();
            var ticketsString = apiPreface + "incremental/tickets.json?start_time=0";
            ticketsRequest.Resource = ticketsString;

            var ticketsJSONResponse = Client.Execute<TicketsJSONResponse>(ticketsRequest);
            if ((int)ticketsJSONResponse.StatusCode == 429)
            {
                var retryAfter = ticketsJSONResponse.Headers.First(x => x.Name == "Retry-After").Value;
                var milisecondsToWait = (int)retryAfter * 1000;
                logger.Error("Rate limit reached, Retry-After: " + (int)retryAfter);
                System.Threading.Thread.Sleep(milisecondsToWait);
                ticketsJSONResponse = Client.Execute<TicketsJSONResponse>(ticketsRequest);
            }
            logger.Info("Request status code: " + ticketsJSONResponse.StatusCode.ToString());

            var tickets = new List<Ticket>();
            tickets.AddRange(ticketsJSONResponse.Data.tickets);
            if (ticketsJSONResponse.Data.count >= 1000)
            {
                var pageNumberTickets = 2;
                do
                {
                    logger.Trace("Page number: " + pageNumberTickets);
                    var nextPageString = apiPreface + "incremental/tickets.json?start_time=" + ticketsJSONResponse.Data.end_time;
                    var nextPageRequest = new RestRequest();
                    nextPageRequest.Resource = nextPageString;
                    ticketsJSONResponse = Client.Execute<TicketsJSONResponse>(nextPageRequest);

                    if ((int)ticketsJSONResponse.StatusCode == 429)
                    {
                        do
                        {
                            var retryAfter = ticketsJSONResponse.Headers.First(x => x.Name == "Retry-After").Value;
                            var milisecondToWait = Convert.ToInt32(retryAfter) * 1000;
                            logger.Error("Rate limit reached, Retry-After: " + (int)retryAfter);
                            System.Threading.Thread.Sleep(milisecondToWait);
                            ticketsJSONResponse = Client.Execute<TicketsJSONResponse>(nextPageRequest);
                        } while ((int)ticketsJSONResponse.StatusCode == 429);
                    }
                    logger.Info("Request status code: " + ticketsJSONResponse.StatusCode.ToString());

                    tickets.AddRange(ticketsJSONResponse.Data.tickets);
                    pageNumberTickets++;
                } while (ticketsJSONResponse.Data.count == 1000);
            }
            logger.Trace("End getAllTicketsIncremental procedure.");
            return tickets;
        }
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static String apiPreface = "api/v2/";

        private String URL;
        private String Username;
        private String Password;
    }
}
