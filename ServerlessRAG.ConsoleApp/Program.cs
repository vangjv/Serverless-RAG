namespace ServerlessRAG.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Select a task to run:");
            Console.WriteLine("1 - RAG example console chat");
            Console.WriteLine("2 - Upload sample docs");
            string input = Console.ReadLine();
            int taskNumber;

            if (int.TryParse(input, out taskNumber))
            {
                switch (taskNumber)
                {
                    case 1:
                        RAGStreamingChat task1 = new RAGStreamingChat();
                        await task1.RunAsync();
                        break;
                    case 2:
                        UploadSampleDocs task2 = new UploadSampleDocs();
                        await task2.RunAsync();
                        break;

                    default:
                        Console.WriteLine("Invalid task number.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a number.");
            }
        }
    }
}
