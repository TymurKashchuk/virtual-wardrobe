using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wardrobe.Application.Common.Interfaces
{
    public interface IEmailSenderService
    {
        Task SendEmailAsync(
            string recipientEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default);
    }
}
