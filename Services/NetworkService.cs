using System.Net.NetworkInformation;

namespace RedOsDeployer.Services;

public static class NetworkService
{
    /// <summary>
    /// Проверяет наличие доступа в интернет путем пинга стабильного узла.
    /// </summary>
    public static async Task<bool> IsOnlineAsync()
    {
        try
        {
            using var ping = new Ping();
            // Пингуем ya.ru с таймаутом в 2 секунды (2000 мс)
            var reply = await ping.SendPingAsync("ya.ru", 2000);

            return reply.Status == IPStatus.Success;
        }
        catch
        {
            // Перехватываем исключение, если сетевой адаптер вообще отключен
            return false;
        }
    }
}