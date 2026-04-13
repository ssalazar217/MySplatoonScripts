using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Stormblood;

public class UCOB_Thunder_Markers : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => null; 
    public override Metadata? Metadata => new(1, "Cant'Load");

    private Config Conf => Controller.GetConfig<Config>();
    private const uint THUNDER_ID = 466;
    private const uint TEST_ID = 50; 
    private HashSet<uint> _markedPlayers = new();
    private bool _active = false;

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Habilitar marcado automatico de Thunder", ref Conf.Enabled);
        ImGui.Checkbox("MODO PRUEBA (Usa Sprint en cualquier zona)", ref Conf.TestMode);
        if (Conf.Enabled)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 1f, 0f, 1f), "Aviso: Si varios jugadores usan este script, pueden entrar en conflicto con las marcas.");
        }
    }

    public class Config : IEzConfig
    {
        public bool Enabled = true;
        public bool TestMode = false;
    }

    public override void OnUpdate()
    {
        if (!Svc.ClientState.IsLoggedIn || Svc.Party.Length == 0 || !Conf.Enabled) return;

        uint statusId = Conf.TestMode ? TEST_ID : THUNDER_ID;

        // Buscar jugadores con el debuff
        var thunderPlayers = Svc.Party
            .Where(p => p.GameObject is IBattleChara chara && chara.StatusList.Any(s => s.StatusId == statusId))
            .Select(p => p.GameObject as IPlayerCharacter)
            .Where(p => p != null)
            .OrderBy(p => p.GetRole()) 
            .ThenBy(p => p.Name.ToString())
            .ToList();

        if (thunderPlayers.Count > 0)
        {
            _active = true;
            for (int i = 0; i < thunderPlayers.Count && i < 2; i++)
            {
                var player = thunderPlayers[i];
                if (!_markedPlayers.Contains(player.EntityId))
                {
                    string markerType = (i == 0) ? "attack1" : "attack2";
                    Svc.Commands.ProcessCommand($"/marker {markerType} \"{player.Name}\"");
                    _markedPlayers.Add(player.EntityId);
                }
            }
        }

        // Limpiar marcas si el jugador ya no tiene el debuff
        if (_active)
        {
            var toRemove = new List<uint>();
            foreach (var entityId in _markedPlayers)
            {
                if (!thunderPlayers.Any(p => p.EntityId == entityId))
                {
                    var player = Svc.Objects.FirstOrDefault(o => o.EntityId == entityId);
                    if (player != null)
                    {
                        Svc.Commands.ProcessCommand($"/marker off \"{player.Name}\"");
                    }
                    toRemove.Add(entityId);
                }
            }

            foreach (var id in toRemove)
            {
                _markedPlayers.Remove(id);
            }

            if (_markedPlayers.Count == 0 && thunderPlayers.Count == 0)
            {
                _active = false;
            }
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        // Limpiar todo en caso de wipe o inicio de pelea
        if (category == DirectorUpdateCategory.Wipe || category == DirectorUpdateCategory.Commence)
        {
            if (_markedPlayers.Count > 0)
            {
                Svc.Commands.ProcessCommand("/marker clear");
            }
            _markedPlayers.Clear();
            _active = false;
        }
    }
}
