using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace GmailTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Prompt the user for email
            Console.WriteLine("Enter your email:");
            string email = Console.ReadLine();
            // Prompt the user for password (masked input)
            Console.WriteLine("Enter your password:");
            string password = GetMaskedInput();

            // Disable Marionette mode
            FirefoxOptions options = new FirefoxOptions();
            options.SetPreference("marionette", false);

            // Instantiate Firefox Driver
            IWebDriver driver = new FirefoxDriver(options);
            // Instantiate WebDriverWait with a timeout of 30 seconds
            WebDriverWait driverWait = new WebDriverWait(new SystemClock(), driver, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1));

            // Navigate to Gmail
            driver.Navigate().GoToUrl("https://www.gmail.com");
            //Wait for email field to be clickable and fill it            
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));
            IWebElement emailField = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("identifierId")));
            emailField.SendKeys(email);

            // Click on the "Next" button
            driver.FindElement(By.Id("identifierNext")).Click();
            // Wait for the password field to be clickable
            IWebElement passwordField = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("input[type='password']")));
            // Fill in password using the correct selector
            passwordField.SendKeys(password);

            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(100);
            // Click on the "Next" button
            driver.FindElement(By.Id("passwordNext")).Click();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(100);

            // Wait for the inbox to load
            wait.Until(driver => driver.Url.Contains("inbox"));

            // Verify default "Primary" section is selected
            try
            {
                IWebElement primaryTab = driver.FindElement(By.XPath("//div[@aria-label='Primary']"));
                if (!primaryTab.GetAttribute("aria-selected").Equals("true"))
                {
                    // Click on the Primary tab if not selected
                    primaryTab.Click();
                }
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Primary tab not found or selected.");
            }

            // Get the total number of emails
            int totalEmailCount = GetTotalEmailCount(driver);
            Console.WriteLine("Number of emails in Primary tab : "+totalEmailCount);

            // head to the proper page
            NavigateToEmailPage(driver, totalEmailCount, totalEmailCount);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
            System.Threading.Thread.Sleep(15000);
            // Example: Get details of the last email
            GetEmailDetailsAtIndex(driver, totalEmailCount);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);

            // Close the browser
            driver.Quit();
        }

        // Method to get masked input for password
        static string GetMaskedInput()
        {
            string input = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                // Handle backspace
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    input += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input = input.Substring(0, (input.Length - 1));
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return input;
        }

        // Method to get the total number of emails in the inbox
        static int GetTotalEmailCount(IWebDriver driver)
        {
            IWebElement spanElement = driver.FindElement(By.XPath("//span[@class='Dj']"));
            string text = spanElement.Text;
            string[] parts = text.Split(" of ");
            string countText = parts[1];
            string[] countParts = countText.Split(" ");
            return int.Parse(countParts[0]);
        }

        // Go to the EmailPage #pageNumber
        static void GoToEmailPage(IWebDriver driver, int pageNumber)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                
                // Repeat the process for each page until reaching the desired page number
                for (int i = 1; i < pageNumber; i++)
                {
                    try
                    {
                        // Wait for the next page ('Older') button to be clickable
                        IWebElement nextPageButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//div[@aria-label='Older']")));
                        // Scroll the next page button into view using JavaScript
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", nextPageButton);
            
                        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
                        // Click on the next page button
                        nextPageButton.Click();
                        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);

                        // Wait for the page to load
                        wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
                        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);

                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine($"Next page button not found or not clickable on page {i}. Exiting.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while navigating to page {pageNumber}: {ex.Message}");
            }
        }

        // Method to get the email element at a specific index on the current page
        static IWebElement GetEmailElementByIndex(IWebDriver driver, int emailIndex)
        {
            try
            {
                return driver.FindElement(By.CssSelector(".zA:nth-of-type(" + emailIndex + ")"));
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Email at index " + emailIndex + " not found.");
                return null;
            }
        }

        // Method to extract and print email details
        static void ExtractEmailDetails(IWebElement emailElement)
        {
            if (emailElement != null)
            {
                string sender = emailElement.FindElement(By.CssSelector(".yW")).Text;
                string subject = emailElement.FindElement(By.CssSelector(".y6")).Text;
                
                Console.WriteLine("Sender: " + sender);
                Console.WriteLine("Subject: " + subject);
            }
        }

        // Method to get and print email details at a specific index
        static void GetEmailDetailsAtIndex(IWebDriver driver, int emailIndex)
        {
            // Calculate the number of emails per page
            int emailsPerPage = 50; // Assuming 50 emails per page

            // Calculate the index of the email on the current page
            int emailIndexOnPage = emailIndex % emailsPerPage;
            if (emailIndexOnPage == 0) emailIndexOnPage = emailsPerPage;

            // Find the email element using its index on the page
            IWebElement emailElement = GetEmailElementByIndex(driver, emailIndexOnPage);

            // Extract the required information from the email element
            ExtractEmailDetails(emailElement);
        }

        static void NavigateToEmailPage(IWebDriver driver, int totalEmailCount, int emailIndex)
        {
            int emailsPerPage = 50; // Assuming 50 emails per page
            int pageNumber = (emailIndex - 1) / emailsPerPage + 1;
            
            if (pageNumber <= 0 || pageNumber > Math.Ceiling((double)totalEmailCount / emailsPerPage))
            {
                Console.WriteLine("Invalid page number.");
                return;
            }
            Console.WriteLine("Heading to page no. " +pageNumber );
            
            GoToEmailPage(driver, pageNumber);
        }

    }
}
