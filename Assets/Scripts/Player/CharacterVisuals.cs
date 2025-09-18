using UnityEngine;

namespace Azurin.Player
{
    /// <summary>
    /// Handles the creation and visual updates of the character model.
    /// This component is responsible for all visual aspects of the player,
    /// separating it from the core player logic.
    /// </summary>
    public class CharacterVisuals : MonoBehaviour
    {
        [Header("Character Parts")]
        private GameObject characterModel;
        private GameObject head;
        private GameObject body;
        private GameObject legs;
        private GameObject arms;

        public void CreateCharacterModel(Transform parent, float height, Color skinColor, Color clothingColor)
        {
            if (characterModel != null)
            {
                Destroy(characterModel);
            }

            characterModel = new GameObject("CossackModel");
            characterModel.transform.SetParent(parent, false);

            head = CreateBodyPart("Head", new Vector3(0, height - 0.15f, 0), new Vector3(0.25f, 0.25f, 0.25f), skinColor);
            body = CreateBodyPart("Body", new Vector3(0, height * 0.6f, 0), new Vector3(0.5f, 0.6f, 0.3f), clothingColor);
            legs = CreateBodyPart("Legs", new Vector3(0, height * 0.25f, 0), new Vector3(0.4f, 0.5f, 0.25f), clothingColor);
            arms = CreateBodyPart("Arms", new Vector3(0, height * 0.7f, 0), new Vector3(0.7f, 0.2f, 0.2f), clothingColor);
            
            var mustache = CreateBodyPart("Mustache", new Vector3(0, 0.05f, 0.13f), new Vector3(0.15f, 0.05f, 0.02f), Color.black);
            mustache.transform.SetParent(head.transform, false);
            mustache.transform.localPosition = new Vector3(0, -0.1f, 0.55f);
        }

        private GameObject CreateBodyPart(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(characterModel.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;

            if (part.TryGetComponent<Collider>(out var col))
            {
                Destroy(col);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            return part;
        }

        public void UpdateHeightVisuals(float newHeight, float originalHeight)
        {
            if (characterModel != null)
            {
                float scaleY = newHeight / originalHeight;
                characterModel.transform.localScale = new Vector3(1f, scaleY, 1f);
            }
        }
    }
} 