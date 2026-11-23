using System;
using System.Collections.Generic;
using System.Linq;

namespace EmployeeExcelAnalytics.Services
{
    public static class PredefinedDepartments
    {
        public const string HumanResources = "Human Resources";
        public const string InformationTechnology = "Information Technology";
        public const string Finance = "Finance";
        public const string Marketing = "Marketing";
        public const string Sales = "Sales";
        public const string Engineering = "Engineering";
        public const string Operations = "Operations";
        public const string Legal = "Legal";
        public const string Administration = "Administration";
        public const string CustomerSupport = "Customer Support";

        public static readonly IReadOnlyList<string> All = new[]
        {
            HumanResources,
            InformationTechnology,
            Finance,
            Marketing,
            Sales,
            Engineering,
            Operations,
            Legal,
            Administration,
            CustomerSupport
        };

        private static readonly Dictionary<string, string[]> DepartmentAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [HumanResources] = new[]
                {
                    "Human Resources", "Human Resource", "HUman resources", "HR", "H.R.", "H.R",
                    "HR Dept", "HR Department", "Personnel", "People", "People Operations",
                    "People & Culture", "Talent Management", "Talent Acquisition", "Talent"
                },

                [InformationTechnology] = new[]
                {
                    "Information Technology", "Information Tech", "Info Tech", "InfoTech",
                    "IT", "I.T.", "I.T", "IT Dept", "IT Department",
                    "Technology", "Tech", "Tech Support", "Information Systems", "IS", "MIS"
                },

                [Finance] = new[]
                {
                    "Finance", "Accounting", "Account", "Accounts", "Finance & Accounting",
                    "Fin", "Fiscal", "Treasury", "Audit", "Financial Services"
                },

                [Marketing] = new[]
                {
                    "Marketing", "Mktg", "Digital Marketing", "Market Research",
                    "Marketing & Communications", "Brand", "Branding", "Advertising", "PR"
                },

                [Sales] = new[]
                {
                    "Sales", "Business Development", "Commercial", "Sales & Marketing",
                    "BizDev", "BD", "Inside Sales", "Direct Sales", "Revenue"
                },

                [Engineering] = new[]
                {
                    "Engineering", "Software Engineering", "Development", "Software Development",
                    "Dev", "Software", "R&D", "Research & Development", "Research and Development",
                    "QA", "Quality Assurance", "Quality Control", "Testing"
                },

                [Operations] = new[]
                {
                    "Operations", "Ops", "Operations Department", "Logistics",
                    "Supply Chain", "Production", "Manufacturing"
                },

                [Legal] = new[]
                {
                    "Legal", "Compliance", "Legal & Compliance", "Regulatory", "Governance"
                },

                [Administration] = new[]
                {
                    "Administration", "Admin", "General Administration", "Facilities",
                    "Office Management", "Administrative"
                },

                [CustomerSupport] = new[]
                {
                    "Customer Support", "Customer Service", "Support", "Client Support",
                    "Customer Success", "Client Success", "Helpdesk", "Service Desk", "Call Center"
                }
            };

        public static bool TryNormalize(string? rawDepartment, out string normalizedDepartment)
        {
            normalizedDepartment = string.Empty;
            if (string.IsNullOrWhiteSpace(rawDepartment))
            {
                return false;
            }

            var cleanedInput = Clean(rawDepartment);

            // 1. Direct or alias match using alphanumeric string comparison
            foreach (var kvp in DepartmentAliases)
            {
                if (Clean(kvp.Key) == cleanedInput)
                {
                    normalizedDepartment = kvp.Key;
                    return true;
                }

                foreach (var alias in kvp.Value)
                {
                    if (Clean(alias) == cleanedInput)
                    {
                        normalizedDepartment = kvp.Key;
                        return true;
                    }
                }
            }

            // 2. Intelligent keyword checks for variations like "HUman resources", "HR team", "IT Solutions"
            var trimmedLower = rawDepartment.Trim().ToLowerInvariant();

            if (trimmedLower == "hr" || trimmedLower.StartsWith("hr ") || trimmedLower.EndsWith(" hr") ||
                (trimmedLower.Contains("human") && trimmedLower.Contains("resource")))
            {
                normalizedDepartment = HumanResources;
                return true;
            }

            if (trimmedLower == "it" || trimmedLower.StartsWith("it ") || trimmedLower.EndsWith(" it") ||
                (trimmedLower.Contains("info") && trimmedLower.Contains("tech")))
            {
                normalizedDepartment = InformationTechnology;
                return true;
            }

            if (trimmedLower.Contains("financ") || trimmedLower.Contains("account"))
            {
                normalizedDepartment = Finance;
                return true;
            }

            if (trimmedLower.Contains("market") || trimmedLower.Contains("branding"))
            {
                normalizedDepartment = Marketing;
                return true;
            }

            if (trimmedLower.Contains("sale") || trimmedLower.Contains("bizdev") || trimmedLower.Contains("business dev"))
            {
                normalizedDepartment = Sales;
                return true;
            }

            if (trimmedLower.Contains("engineer") || trimmedLower.Contains("software") || trimmedLower.Contains("develop"))
            {
                normalizedDepartment = Engineering;
                return true;
            }

            if (trimmedLower.Contains("operat") || trimmedLower.Contains("logistics") || trimmedLower.Contains("supply chain"))
            {
                normalizedDepartment = Operations;
                return true;
            }

            if (trimmedLower.Contains("legal") || trimmedLower.Contains("complian"))
            {
                normalizedDepartment = Legal;
                return true;
            }

            if (trimmedLower.Contains("admin") || trimmedLower.Contains("facilit"))
            {
                normalizedDepartment = Administration;
                return true;
            }

            if (trimmedLower.Contains("support") || trimmedLower.Contains("customer") || trimmedLower.Contains("helpdesk"))
            {
                normalizedDepartment = CustomerSupport;
                return true;
            }

            return false;
        }

        private static string Clean(string text)
        {
            return new string(
                text.Where(char.IsLetterOrDigit)
                    .Select(char.ToLowerInvariant)
                    .ToArray()
            );
        }
    }
}
