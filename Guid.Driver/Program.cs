using Guid.Driver.Proxies;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Guid.Driver
{
    class Program
    {
        private const int _exitOption = 10;

        static async Task Main(string[] args)
        {
            int option;
            do
            {
                option = DisplayMenu();
                if (option != _exitOption)
                {
                    Console.WriteLine();
                    switch (option)
                    {
                        case 1:
                            await CreateGuidAsync();
                            break;

                        case 2:
                            await ReadGuidAsync();
                            break;

                        case 3:
                            await UpdateGuidAsync();
                            break;

                        case 4:
                            await DeleteGuidAsync();
                            break;
                    }
                    Console.WriteLine();
                }
            } while (option != 5);
        }

        private async static Task DeleteGuidAsync()
        {
            Console.Write("Enter guid to delete: ");
            if (ReadGuid(out System.Guid? guid, false))
            {
                var proxy = new GuidInfosProxy(HttpClient.Instance);
                try
                {
                    await proxy.DeleteGuidInfoAsync(guid.Value);
                    Console.WriteLine("Successfully deleted.");
                }
                catch (GuidApiException<GuidApiError> ex)
                {
                    DisplayError(ex, ex.Result);
                }
                catch (GuidApiException ex)
                {
                    DisplayError(ex);
                }
            }
        }

        private async static Task UpdateGuidAsync()
        {
            Console.Write("Enter guid to update: ");
            if (ReadGuid(out System.Guid? guid, false))
            {
                var info = new GuidInfoBase();
                Console.Write("Enter user name: ");
                info.User = Console.ReadLine();
                Console.Write("Enter expiration date/time (mm/dd/yyyy hh/mm/ss): ");
                if (ReadDateTime(out DateTime? date))
                {
                    info.Expire = date;
                    var proxy = new GuidInfosProxy(HttpClient.Instance);
                    try
                    {
                        var guidInfo = await proxy.CreateOrUpdateGuidInfoAsync(guid.Value, info);
                        DisplayGuidInfo(guidInfo);
                    }
                    catch (GuidApiException<GuidApiError> ex)
                    {
                        DisplayError(ex, ex.Result);
                    }
                    catch (GuidApiException ex)
                    {
                        DisplayError(ex);
                    }
                }
            }
        }

        private async static Task ReadGuidAsync()
        {
            Console.Write("Enter guid to read: ");
            if (ReadGuid(out System.Guid? guid, false))
            {
                var proxy = new GuidInfosProxy(HttpClient.Instance);
                try
                {
                    var guidInfo = await proxy.GetGuidInfoAsync(guid.Value);
                    DisplayGuidInfo(guidInfo);
                }
                catch (GuidApiException<GuidApiError> ex)
                {
                    DisplayError(ex, ex.Result);
                }
                catch (GuidApiException ex)
                {
                    DisplayError(ex);
                }
            }
        }

        private static async Task CreateGuidAsync()
        {
            var info = new GuidInfoBase();
            Console.Write("Enter guid to create (skip generates new guid): ");
            if (ReadGuid(out System.Guid? guid))
            {
                Console.Write("Enter user name: ");
                info.User = Console.ReadLine();
                Console.Write("Enter expiration date/time (mm/dd/yyyy hh/mm/ss) or skip for 30 day default: ");
                if (ReadDateTime(out DateTime? date))
                {
                    info.Expire = date;
                    var proxy = new GuidInfosProxy(HttpClient.Instance);
                    try
                    {
                        var guidInfo = !guid.HasValue ?
                            await proxy.CreateGuidInfoAsync(info) :
                            await proxy.CreateOrUpdateGuidInfoAsync(guid.Value, info);
                        DisplayGuidInfo(guidInfo);
                    }
                    catch (GuidApiException<GuidApiError> ex)
                    {
                        DisplayError(ex, ex.Result);
                    }
                    catch (GuidApiException ex)
                    {
                        DisplayError(ex);
                    }
                }
            }
        }

        private static bool ReadDateTime(out DateTime? date)
        {
            date = null;
            var input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
            {
                if (DateTime.TryParse(input, out DateTime expire) && 
                    expire >= new DateTime(1970, 1, 1, 0, 0, 0))
                {
                    // must be compliant with UNIX date/times
                    date = expire;
                    return true;
                }
                else
                {
                    Console.WriteLine("Invalid expiration date.");
                    return false;
                }
            }

            return true;
        }

        private static bool ReadGuid(out System.Guid? guid, bool optional = true)
        {
            guid = null;
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) && optional)
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(input) && 
                System.Guid.TryParse(input, out System.Guid id))
            {
                guid = id;
                return true;
            }
            else
            {
                Console.WriteLine("Invalid guid.");
                return false;
            }
        }

        private static void DisplayError(GuidApiException ex, GuidApiError result = null)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("ERROR: ");
            if (result == null)
            {
                // use generic exception message
                builder.Append(ex.Message);
            }
            else
            {
                // use specific API error message
                if (result.Code == GuidErrorCode.InvalidUser)
                {
                    builder.Append("Invalid user.");
                }
                else if (result.Code == GuidErrorCode.GuidExpired)
                {
                    builder.Append("Guid expired.");
                }
                else if (result.Code == GuidErrorCode.GuidNotFound)
                {
                    builder.Append("Guid not found.");
                }
                else
                {
                    builder.Append("An error occurred.");
                }
                if (!string.IsNullOrEmpty(result.Details))
                {
                    var details = result.Details.Trim();
                    builder.Append($" {details}");
                    if (!details.EndsWith("."))
                    {
                        builder.Append(".");
                    }
                }
            }

            builder.Append($" (Http Code: {ex.StatusCode})");
            Console.WriteLine();
            Console.WriteLine(builder.ToString());
        }

        private static void DisplayGuidInfo(GuidInfo guidInfo)
        {
            Console.WriteLine();
            Console.WriteLine($"Guid: {guidInfo.Guid}");
            Console.WriteLine($"Expires On: {guidInfo.Expire.Value.ToLocalTime().ToString()}");
            Console.WriteLine($"User: {guidInfo.User}");
        }

        private static int DisplayMenu()
        {
            Console.WriteLine("What would you like to do?");
            Console.WriteLine();
            Console.WriteLine("1. Create guid");
            Console.WriteLine("2. Read guid");
            Console.WriteLine("3. Update guid");
            Console.WriteLine("4. Delete guid");
            Console.WriteLine($"{_exitOption}. Exit");
            Console.WriteLine();
            Console.Write("Enter option: ");

            return Convert.ToInt32(Console.ReadLine());
        }
    }
}
