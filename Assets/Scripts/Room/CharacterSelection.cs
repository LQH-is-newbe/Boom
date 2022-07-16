using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class CharacterSelection : NetworkBehaviour {
    public GameObject characterSelectionUnitPrefab;
    private Dictionary<string, CharacterSelectionUnit> units = new();

    public void Init() {
        for (int i = 0; i < Static.characters.Length; i++) {
            GameObject characterSelectionUnit = Instantiate(characterSelectionUnitPrefab);
            characterSelectionUnit.transform.SetParent(gameObject.transform);
            CharacterSelectionUnit controller = characterSelectionUnit.GetComponent<CharacterSelectionUnit>();
            controller.characterName.Value = new FixedString64Bytes(Static.characters[i]);
            controller.x.Value = -465 + 360 * i;
            characterSelectionUnit.GetComponent<NetworkObject>().Spawn();
            units.Add(Static.characters[i], controller);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SelectCharacterServerRpc(ulong clientId, string characterName) {
        CharacterSelectionUnit unit = units[characterName];
        if (unit.selected.Value) {
            if (Static.playerCharacters[clientId] == characterName) {
                Static.playerCharacters.Remove(clientId);
                unit.selected.Value = false;
                unit.playerName.Value = new FixedString64Bytes("");
            }
        } else {
            string previousCharacter = "";
            if (Static.playerCharacters.TryGetValue(clientId, out previousCharacter)) {
                CharacterSelectionUnit previousUnit = units[previousCharacter];
                Static.playerCharacters.Remove(clientId);
                previousUnit.selected.Value = false;
                previousUnit.playerName.Value = new FixedString64Bytes("");
            }
            Static.playerCharacters[clientId] = characterName;
            unit.selected.Value = true;
            unit.playerName.Value =  Static.playerNames[clientId];
        }
    }
}
