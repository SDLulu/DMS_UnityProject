using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AIInspectionTools
{
    internal static class InspectionUtility
    {
        public static GameObject GetSelectedGameObject(Object selected)
        {
            return selected switch
            {
                GameObject gameObject => gameObject,
                Component component => component.gameObject,
                _ => null
            };
        }

        public static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return null;
            }

            var names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        public static object DescribeObject(Object obj)
        {
            if (obj == null)
            {
                return null;
            }

            string assetPath = AssetDatabase.GetAssetPath(obj);
            GameObject gameObject = GetSelectedGameObject(obj);
            return new
            {
                name = obj.name,
                type = obj.GetType().Name,
                asset_path = string.IsNullOrEmpty(assetPath) ? null : assetPath,
                scene_path = gameObject != null && gameObject.scene.IsValid() ? GetHierarchyPath(gameObject.transform) : null
            };
        }

        public static object DescribeGameObject(GameObject gameObject, int depth, int childLimit, int fieldLimit)
        {
            if (gameObject == null)
            {
                return null;
            }

            string prefabSourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            GameObject nearestPrefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);

            return new
            {
                name = gameObject.name,
                scene = gameObject.scene.IsValid() ? gameObject.scene.name : null,
                scene_path = gameObject.scene.IsValid() ? gameObject.scene.path : null,
                hierarchy_path = GetHierarchyPath(gameObject.transform),
                active_self = gameObject.activeSelf,
                active_in_hierarchy = gameObject.activeInHierarchy,
                tag = gameObject.tag,
                layer = LayerMask.LayerToName(gameObject.layer),
                prefab_source_path = string.IsNullOrEmpty(prefabSourcePath) ? null : prefabSourcePath,
                nearest_prefab_root = nearestPrefabRoot != null ? GetHierarchyPath(nearestPrefabRoot.transform) : null,
                transform = DescribeTransform(gameObject.transform),
                children = DescribeChildren(gameObject.transform, depth, childLimit),
                components = DescribeComponents(gameObject, fieldLimit)
            };
        }

        public static object DescribeAsset(Object obj)
        {
            if (obj == null)
            {
                return null;
            }

            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return new
            {
                name = obj.name,
                type = obj.GetType().Name,
                path,
                guid = AssetDatabase.AssetPathToGUID(path),
                is_folder = AssetDatabase.IsValidFolder(path),
                extension = Path.GetExtension(path),
                main_asset_type = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name,
                labels = AssetDatabase.GetLabels(obj)
            };
        }

        public static List<string> GetSelectedAssetPaths(bool expandFolders)
        {
            var paths = new SortedSet<string>();
            foreach (Object selected in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (string.IsNullOrEmpty(path))
                {
                    GameObject gameObject = GetSelectedGameObject(selected);
                    if (gameObject != null)
                    {
                        path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                    }
                }

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (expandFolders && AssetDatabase.IsValidFolder(path))
                {
                    foreach (string guid in AssetDatabase.FindAssets("", new[] { path }))
                    {
                        string childPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(childPath) && !AssetDatabase.IsValidFolder(childPath))
                        {
                            paths.Add(childPath);
                        }
                    }
                    continue;
                }

                paths.Add(path);
            }

            return new List<string>(paths);
        }

        private static object DescribeTransform(Transform transform)
        {
            return new
            {
                local_position = ToArray(transform.localPosition),
                local_rotation_euler = ToArray(transform.localEulerAngles),
                local_scale = ToArray(transform.localScale),
                world_position = ToArray(transform.position)
            };
        }

        private static List<object> DescribeChildren(Transform transform, int depth, int childLimit)
        {
            var children = new List<object>();
            if (transform == null || depth <= 0)
            {
                return children;
            }

            int count = Mathf.Min(transform.childCount, Mathf.Max(0, childLimit));
            for (int i = 0; i < count; i++)
            {
                Transform child = transform.GetChild(i);
                children.Add(new
                {
                    name = child.name,
                    active_self = child.gameObject.activeSelf,
                    component_types = GetComponentTypeNames(child.gameObject),
                    children = DescribeChildren(child, depth - 1, childLimit)
                });
            }

            if (transform.childCount > count)
            {
                children.Add(new { truncated_children = transform.childCount - count });
            }

            return children;
        }

        private static List<object> DescribeComponents(GameObject gameObject, int fieldLimit)
        {
            var components = new List<object>();
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component == null)
                {
                    components.Add(new { type = "MissingScript" });
                    continue;
                }

                components.Add(new
                {
                    type = component.GetType().Name,
                    enabled = component is Behaviour behaviour ? behaviour.enabled : (bool?)null,
                    fields = DescribeSerializedFields(component, fieldLimit)
                });
            }

            return components;
        }

        private static List<object> DescribeSerializedFields(Object target, int fieldLimit)
        {
            var fields = new List<object>();
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            int count = 0;
            int max = Mathf.Max(0, fieldLimit);

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name == "m_Script")
                {
                    continue;
                }

                if (count >= max)
                {
                    fields.Add(new { truncated_fields = true });
                    break;
                }

                fields.Add(new
                {
                    path = property.propertyPath,
                    type = property.propertyType.ToString(),
                    value = GetPropertyValue(property)
                });
                count++;
            }

            return fields;
        }

        private static object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Color:
                    return ToArray(property.colorValue);
                case SerializedPropertyType.ObjectReference:
                    return DescribeObject(property.objectReferenceValue);
                case SerializedPropertyType.LayerMask:
                    return property.intValue;
                case SerializedPropertyType.Enum:
                    return property.enumDisplayNames != null
                        && property.enumValueIndex >= 0
                        && property.enumValueIndex < property.enumDisplayNames.Length
                            ? property.enumDisplayNames[property.enumValueIndex]
                            : property.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return ToArray(property.vector2Value);
                case SerializedPropertyType.Vector3:
                    return ToArray(property.vector3Value);
                case SerializedPropertyType.Vector4:
                    return ToArray(property.vector4Value);
                case SerializedPropertyType.Rect:
                    return new[] { property.rectValue.x, property.rectValue.y, property.rectValue.width, property.rectValue.height };
                case SerializedPropertyType.Bounds:
                    return new
                    {
                        center = ToArray(property.boundsValue.center),
                        size = ToArray(property.boundsValue.size)
                    };
                case SerializedPropertyType.Quaternion:
                    return ToArray(property.quaternionValue.eulerAngles);
                default:
                    return property.hasVisibleChildren ? "<complex>" : property.displayName;
            }
        }

        private static List<string> GetComponentTypeNames(GameObject gameObject)
        {
            var types = new List<string>();
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                types.Add(component != null ? component.GetType().Name : "MissingScript");
            }

            return types;
        }

        private static float[] ToArray(Vector2 value)
        {
            return new[] { value.x, value.y };
        }

        private static float[] ToArray(Vector3 value)
        {
            return new[] { value.x, value.y, value.z };
        }

        private static float[] ToArray(Vector4 value)
        {
            return new[] { value.x, value.y, value.z, value.w };
        }

        private static float[] ToArray(Color value)
        {
            return new[] { value.r, value.g, value.b, value.a };
        }
    }
}
