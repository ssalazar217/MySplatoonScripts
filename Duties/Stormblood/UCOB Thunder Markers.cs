using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Stormblood
{
    public class UCOB_Thunder_Markers : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => [Raids.the_Unending_Coil_of_Bahamut_Ultimate];
        public override Metadata? Metadata => new(1, "CantLoad");

        private Config Conf => Controller.GetConfig<Config>();
        private const uint THUNDER_ID = 466;
        private const uint TEST_ID = 50; 
        private HashSet<uint> _markedPlayers = [];
        private bool _active = false;

        public override void OnSettingsDraw()
        {
            ImGui.Checkbox("Habilitar marcado automatico de Thunder", ref Conf.Enabled);
            ImGui.Checkbox("MODO PRUEBA (Usa Sprint)", ref Conf.TestMode);
            if (Conf.Enabled)
            {
                ImGui.TextColored(new Vector4(1f, 1f, 0f, 1f), "Aviso: Evita que varios usuarios usen el script a la vez.");
            }
            if (Conf.TestMode)
            {
                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ATENCION: El MODO PRUEBA requiere entrar en UCOB para que el script se cargue.");
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
                        Svc.Commands.ProcessCommand($"/marker {markerType} \"{player.Name.ToString()}\"");
                        _markedPlayers.Add(player.EntityId);
                    }
                }
            }

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
                            Svc.Commands.ProcessCommand($"/marker off \"{player.Name.ToString()}\"");
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
}
