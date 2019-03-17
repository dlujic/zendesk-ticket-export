using System;
using System.ComponentModel.DataAnnotations;

namespace zendesk_ticket_export.Models
{
    public class ExportTicketsViewModel
    { 
        [Required]
        [Display(Name = "Zendesk URL")]
        public string ZendeskURL { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Access Token")]
        public string AccessToken { get; set; }
    }
}