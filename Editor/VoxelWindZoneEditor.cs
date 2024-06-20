using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VoxelWind;

namespace VoxelWindEditor
{
    [CustomEditor(typeof(VoxelWindZone))]
    public class VoxelWindZoneEditor : Editor
    {
        VoxelWindZone voxelWindZone;

        void OnEnable()
        {
            voxelWindZone = target as VoxelWindZone;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var inspector = InstantiateInspectorUI();
            root.Add(inspector);

            SetupVoxelFieldCallbacks(inspector);

            SetupShaderFieldCallbacks(inspector);

            SetupAutoAssignShaderCallbacks(inspector);

            return root;
        }

        private void SetupVoxelFieldCallbacks(TemplateContainer inspector)
        {
            var voxelSizeField = inspector.Q<FloatField>("VoxelSize");
            var voxelGridSizeField = inspector.Q<Vector3Field>("VoxelGridSize");
            var voxelDensityField = inspector.Q<Vector3IntField>("VoxelDensity");
            var voxelSizeWarning = inspector.Q<HelpBox>("VoxelSizeWarning");

            voxelSizeField.RegisterValueChangedCallback(evt =>
            {
                var mx = voxelWindZone.voxelGridSize.x % evt.newValue;
                var my = voxelWindZone.voxelGridSize.y % evt.newValue;
                var mz = voxelWindZone.voxelGridSize.z % evt.newValue;

                if (mx != 0 || my != 0 || mz != 0)
                    voxelSizeWarning.style.display = DisplayStyle.Flex;
                else
                    voxelSizeWarning.style.display = DisplayStyle.None;

                var voxelDensity = new Vector3Int((int)(voxelWindZone.voxelGridSize.x / evt.newValue), (int)(voxelWindZone.voxelGridSize.y / evt.newValue), (int)(voxelWindZone.voxelGridSize.z / evt.newValue));
                voxelDensityField.value = voxelDensity;
            });

            voxelGridSizeField.RegisterValueChangedCallback(evt =>
            {
                var mx = evt.newValue.x % voxelWindZone.voxelSize;
                var my = evt.newValue.y % voxelWindZone.voxelSize;
                var mz = evt.newValue.z % voxelWindZone.voxelSize;

                if (mx != 0 || my != 0 || mz != 0)
                    voxelSizeWarning.style.display = DisplayStyle.Flex;
                else
                    voxelSizeWarning.style.display = DisplayStyle.None;

                var voxelDensity = new Vector3Int((int)(evt.newValue.x / voxelWindZone.voxelSize), (int)(evt.newValue.y / voxelWindZone.voxelSize), (int)(evt.newValue.z / voxelWindZone.voxelSize));
                voxelDensityField.value = voxelDensity;
            });
        }

        private static void SetupShaderFieldCallbacks(TemplateContainer inspector)
        {
            var packingShaderFiled = inspector.Q<ObjectField>("PackingShader");
            var packingShaderHelpBox = inspector.Q<HelpBox>("PackingShaderWarning");
            var updatingShaderFiled = inspector.Q<ObjectField>("UpdatingShader");
            var updatingShaderHelpBox = inspector.Q<HelpBox>("UpdatingShaderWarning");

            packingShaderFiled.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null)
                    packingShaderHelpBox.style.display = DisplayStyle.Flex;
                else
                    packingShaderHelpBox.style.display = DisplayStyle.None;
            });

            updatingShaderFiled.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null)
                    updatingShaderHelpBox.style.display = DisplayStyle.Flex;
                else
                    updatingShaderHelpBox.style.display = DisplayStyle.None;
            });
        }

        private void SetupAutoAssignShaderCallbacks(TemplateContainer inspector)
        {
            var autoAssignShaderButton = inspector.Q<Button>("AutoAssignShader");

            autoAssignShaderButton.clicked += () =>
            {
                voxelWindZone.packingShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.misaki.voxel-wind/Runtime/Shader/ComputeShader/PackIntoTexture.compute");
                voxelWindZone.updatingShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.misaki.voxel-wind/Runtime/Shader/ComputeShader/UpdateVoxels.compute");
            };
        }

        private static TemplateContainer InstantiateInspectorUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.misaki.voxel-wind/Editor/View/VoxelWindZoneView.uxml");
            var inspector = visualTree.Instantiate();
            return inspector;
        }
    }
}
