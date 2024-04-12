using System.Net.Sockets;

public class Cliente
{
	public static void Main(string[] args)
	{
		string server = args.Length > 0 ? args[0] : "127.0.0.1"; // Get server address from args or default to localhost
		int port = 8888; // Server port

		try
		{
			using (TcpClient client = new TcpClient(server, port))
			using (StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true })
			using (StreamReader reader = new StreamReader(client.GetStream()))
			{
				// Wait for server's initial response
				string serverResponse = reader.ReadLine();
				HandleServerResponse(serverResponse); // Handle initial response

				// Communication loop
				string input = "";
				while (input.ToUpper() != "QUIT")
				{
					Console.Write("> ");
					input = Console.ReadLine();
					writer.WriteLine(input);

					serverResponse = reader.ReadLine();
					HandleServerResponse(serverResponse); // Handle each response

					if (input.ToUpper() == "QUIT")
					{
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Erro: {ex.Message}");
		}
	}

	private static void HandleServerResponse(string response)
	{
		if (!string.IsNullOrEmpty(response))
		{
			string[] parts = response.Split(' ');
			string code = parts[0];
			string message = string.Join(" ", parts, 1, parts.Length - 1);

			switch (code)
			{
				case "200":
					Console.WriteLine("OK: " + message);
					break;

				case "400":
					Console.WriteLine("Erro: " + message);
					break;

				case "404":
					Console.WriteLine("Não encontrado: " + message);
					break;

				case "500":
					Console.WriteLine("Erro de servidor: " + message);
					break;

				default:
					Console.WriteLine("Resposta desconhecida: " + response);
					break;
			}
		}
	}
}