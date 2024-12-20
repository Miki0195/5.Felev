using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WordQuizApp
{
    class Program
    {
        static Dictionary<string, string> wordPairs = new Dictionary<string, string>();
        static Random random = new Random();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            string filePath = "words.txt";
            LoadWords(filePath);

            Console.WriteLine("Choose mode: \n1. Hungarian to English \n2. English to Hungarian");
            string choice = Console.ReadLine();
            bool isHungarianToEnglish = choice == "1";

            StartQuiz(isHungarianToEnglish);
        }

        static void LoadWords(string filePath)
        {
            try
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    if (line.Contains("-"))
                    {
                        var parts = line.Split('-');
                        string hungarian = parts[0].Trim();
                        string english = parts[1].Trim();
                        wordPairs[hungarian] = english;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading words: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void StartQuiz(bool isHungarianToEnglish)
        {
            Console.WriteLine("Starting the quiz! Type 'exit' to quit.\n");

            while (true)
            {
                var wordPair = wordPairs.ElementAt(random.Next(wordPairs.Count));
                string question = isHungarianToEnglish ? wordPair.Key : wordPair.Value;
                string correctAnswer = isHungarianToEnglish ? wordPair.Value : wordPair.Key;

                Console.WriteLine(question);
                string userInput = Console.ReadLine();

                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Thanks for playing! Goodbye!");
                    break;
                }

                if (userInput.Trim().Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Correct! Well done!\n");
                }
                else
                {
                    Console.WriteLine($"Incorrect. The correct answer is: {correctAnswer}\n");
                }
            }
        }
    }
}
