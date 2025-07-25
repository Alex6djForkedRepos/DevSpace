﻿using DevSpaceShared;
using DevSpaceShared.Data;
using DevSpaceShared.Events.Docker;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Text.Json;

namespace DevSpaceAgent.Docker;

public static class DockerNetworks
{
    public static async Task<IList<DockerNetworkInfo>> ListNetworksAsync(DockerClient client)
    {
        IList<DockerNetworkInfo> List = [];
        IList<NetworkResponse> Networks = await client.Networks.ListNetworksAsync();
        IList<ContainerListResponse> Containers = await client.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true
        });

        foreach (NetworkResponse i in Networks)
        {
            DockerNetworkInfo Network = DockerNetworkInfo.Create(i, false);

            foreach (ContainerListResponse? c in Containers.Where(x => x.NetworkSettings.Networks != null && x.NetworkSettings.Networks.ContainsKey(i.Name)))
            {
                Network.ContainersCount += 1;
                if (c.Names != null && c.Names.Any())
                    Network.Containers.Add(c.ID, c.Names.First().Substring(1));
                else
                    Network.Containers.Add(c.ID, c.ID);
            }
            List.Add(Network);
        }
        return List;
    }

    public static async Task CreateNetworkAsync(DockerClient client, CreateNetworkEvent? data)
    {
        if (data == null)
            throw new Exception("Network creation options is missing.");

        NetworksCreateParameters Create = new NetworksCreateParameters
        {
            Name = data.Name,
            Driver = data.DriverType,
            Options = data.DriverOptions,
            Labels = data.Labels,
            Attachable = data.IsManualAttach,
            Internal = data.IsIsolated,
            CheckDuplicate = true
        };
        if (!string.IsNullOrEmpty(data.IPv4Gateway) || !string.IsNullOrEmpty(data.IPv4Range) || !string.IsNullOrEmpty(data.IPv4Subnet))
        {
            Create.IPAM.Config.Add(new IPAMConfig
            {
                IPRange = data.IPv4Range,
                Gateway = data.IPv4Gateway,
                Subnet = data.IPv4Subnet,
            });
        }
        if (!string.IsNullOrEmpty(data.IPv6Gateway) || !string.IsNullOrEmpty(data.IPv6Range) || !string.IsNullOrEmpty(data.IPv6Subnet))
        {
            Create.IPAM.Config.Add(new IPAMConfig
            {
                IPRange = data.IPv6Range,
                Gateway = data.IPv6Gateway,
                Subnet = data.IPv6Subnet,
            });
        }

        await client.Networks.CreateNetworkAsync(Create);
    }

    public static async Task<object?> ControlNetworkAsync(DockerClient client, DockerEvent @event, string id)
    {
        switch (@event.NetworkType)
        {
            case ControlNetworkType.View:
                NetworkResponse Network = await client.Networks.InspectNetworkAsync(id);
                NetworkResponse? ParentNetwork = null;
                if (Network.ConfigFrom != null && !string.IsNullOrEmpty(Network.ConfigFrom.Network))
                {
                    try
                    {
                        ParentNetwork = await client.Networks.InspectNetworkAsync(Network.ConfigFrom.Network);
                    }
                    catch { }
                }
                DockerNetworkInfo Data = DockerNetworkInfo.Create(Network, true, ParentNetwork);

                IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(new ContainersListParameters()
                {
                    All = true,
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                                {
                                    { "network", new Dictionary<string, bool>
                                    {
                                        { id, true }
                                    }
                                    }
                                }
                });

                foreach (ContainerListResponse i in containers)
                {
                    if (!Data.ContainersList.ContainsKey(i.ID))
                    {
                        if (i.NetworkSettings.Networks.TryGetValue(Network.Name, out EndpointSettings? endpoint))
                        {
                            Data.ContainersList.Add(i.ID, new EndpointResource
                            {
                                Name = i.Names.Any() ? i.Names.First().Substring(1) : i.ID,
                                IPv4Address = endpoint.IPAddress,
                                IPv6Address = endpoint.GlobalIPv6Address,
                                MacAddress = endpoint.MacAddress,
                                EndpointID = endpoint.EndpointID
                            });
                        }
                        else
                        {
                            Data.ContainersList.Add(i.ID, new EndpointResource
                            {
                                Name = i.Names.Any() ? i.Names.First().Substring(1) : i.ID
                            });
                        }
                    }
                }

                return Data;
            case ControlNetworkType.Remove:
                await client.Networks.DeleteNetworkAsync(id);
                break;
            case ControlNetworkType.JoinNetwork:
                {
                    DockerContainerInfo? Container = @event.Data?.Deserialize<DockerContainerInfo>(AgentJsonOptions.Options);
                    if (Container == null)
                        throw new Exception("Container id is missing.");

                    await client.Networks.ConnectNetworkAsync(id, new NetworkConnectParameters
                    {
                        Container = Container.Id,
                        EndpointConfig = new EndpointSettings
                        {
                            NetworkID = id
                        }
                    });
                }
                break;
            case ControlNetworkType.LeaveNetwork:
                {
                    DockerContainerInfo? Container = @event.Data?.Deserialize<DockerContainerInfo>(AgentJsonOptions.Options);
                    if (Container == null)
                        throw new Exception("Container id is missing.");

                    await client.Networks.DisconnectNetworkAsync(id, new NetworkDisconnectParameters
                    {
                        Container = Container.Id
                    });
                }
                break;
        }
        return null;
    }
}
