// Точка входа в программу. Выводим приветственную строку
Console.WriteLine("");
Console.WriteLine("==============================");
Console.WriteLine("==                          ==");
Console.WriteLine("== Система учёта материалов ==");
Console.WriteLine("== Серверное приложение v.1 ==");
Console.WriteLine("==                          ==");
Console.WriteLine("==============================");
Console.WriteLine("");

// Запускаем непосредственно сервер.
ServerApp.ServerApp.Run(args);