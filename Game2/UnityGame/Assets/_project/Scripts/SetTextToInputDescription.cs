using NonStandard.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SetTextToInputDescription : MonoBehaviour {
    void DoTheThing() {
        Text t = GetComponent<Text>();
        t.text = UserInput.GetInputDescription();
    }
    private void Start() {
        DoTheThing();
        InputSystem.onSettingsChange += DoTheThing;
        InputControlBinding.OnActiveChange += DoTheThing;
    }
    private void OnDestroy() {
        InputSystem.onSettingsChange -= DoTheThing;
        InputControlBinding.OnActiveChange -= DoTheThing;
    }
    private void OnEnable() {
        DoTheThing();
    }
}
