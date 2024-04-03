using System.Net.Sockets;

public class Cliente
{
	public static void Main(string[] args)
	{
		string server = "127.0.0.1"; // Endereço IP do servidor
		int port = 8888; // Porta do servidor

		TcpClient client = new TcpClient(server, port);
		StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
		StreamReader reader = new StreamReader(client.GetStream());

		Console.WriteLine("Conectado ao servidor. Enviando ID...");
		writer.WriteLine("ID 123"); // Exemplo de ID

		// Loop para interação com o servidor, pode ser encerrado com "QUIT"
		string input = "";
		while (input.ToUpper() != "QUIT")
		{
			input = Console.ReadLine();
			writer.WriteLine(input);
			if (input.ToUpper() == "QUIT")
			{
				Console.WriteLine(reader.ReadLine());
				break;
			}
		}

		client.Close();
	}
}