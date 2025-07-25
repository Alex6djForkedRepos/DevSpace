﻿using DevSpaceWeb.Data.Servers;

namespace DevSpaceWeb.API.Servers;

public class ServerJson
{
    public ServerJson(ServerData data, bool showIp)
    {
        id = data.Id.ToString();
        name = data.Name;
        owner_id = data.OwnerId.ToString();
        vanity_url = data.VanityUrl;
        created_at = data.CreatedAt;
        is_online = data.IsConnected;
        if (showIp)
            ip = data.AgentIp;
    }

    public string id { get; set; }
    public string name { get; set; }
    public string owner_id { get; set; }
    public string? vanity_url { get; set; }
    public DateTime created_at { get; set; }
    public bool is_online { get; set; }
    public string? ip { get; set; }
}
