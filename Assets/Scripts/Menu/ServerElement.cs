using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

class ServerElement : VisualElement
{
    protected Label serverNameLabel, numPlayersLabel, mapNameLabel;

    public ServerElement()
    {
        serverNameLabel = new Label();
        mapNameLabel = new Label();
        numPlayersLabel = new Label();

        serverNameLabel.style.width = Length.Percent(40);
        mapNameLabel.style.width = Length.Percent(40);
        numPlayersLabel.style.width = Length.Percent(20);

        Add(serverNameLabel);
        Add(mapNameLabel);
        Add(numPlayersLabel);

        style.flexGrow = 1;
        style.flexShrink = 1;
        style.flexDirection = FlexDirection.Row;
    }

    public void UpdateServer(string name, string mapName, int numPLayers, int maxPlayers)
    {
        serverNameLabel.text = name;
        mapNameLabel.text = mapName;
        numPlayersLabel.text = $"{numPLayers}/{maxPlayers}";
    }


}
