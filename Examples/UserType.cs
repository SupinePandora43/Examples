﻿using GmodNET.API;
using System;
using System.Runtime.InteropServices;

namespace Examples
{
    /// <summary>
    /// this module wraps c# string to lua
    /// </summary>
    /// TODO: rename to csstring?
    public class UserType : IModule
    {
        public string ModuleName => "UserType";

        public string ModuleVersion => "1.0.0";

        private static int UserType_Id;

        public void Load(ILua lua, bool is_serverside, ModuleAssemblyLoadContext assembly_context)
        {
            // create usertype identifier
            UserType_Id = lua.CreateMetaTable("Example_UserType");
            // set meta-methods
            // see http://www.tutorialspoint.com/lua/lua_metatables.htm for all meta-methods
            // also checkout https://www.lua.org/pil/13.html for explanation about meta-methods/tables

            // __index method
            // __index is called when you trying to index usertype ex:
            // MyCSString.Length
            // MyCSString:ToCharArray()
            // FindMetaTable("Example_UserType").ToCharArray(MyCSString)
            lua.PushManagedFunction((lua) =>
            {
                IntPtr ptr = lua.GetUserType(1, UserType_Id);
                if (ptr != IntPtr.Zero)
                {
                    string indexingname = lua.GetString(2);
                    switch (indexingname)
                    {
                        // pushing simple number
                        case "Length":
                            GCHandle gCHandle = GCHandle.FromIntPtr(ptr);
                            string csString = (string)gCHandle.Target;
                            lua.PushNumber(csString.Length);
                            break;
                        // pushing function
                        case "ToCharArray":
                            lua.PushManagedFunction((lua) =>
                            {
                                IntPtr ptr = lua.GetUserType(1, UserType_Id);
                                GCHandle gCHandle = GCHandle.FromIntPtr(ptr);
                                string csString = (string)gCHandle.Target;
                                lua.CreateTable();
                                char[] charArray = csString.ToCharArray();
                                for (int i = 0; i < charArray.Length; i++)
                                {
                                    lua.PushNumber(i);
                                    lua.PushString(charArray[i].ToString());
                                    lua.SetTable(-3);
                                }
                                return 1;
                            });
                            break;
                        case "Clone":
                            lua.PushManagedFunction((lua) =>
                            {
                                IntPtr ptr1 = lua.GetUserType(1, UserType_Id);
                                GCHandle gCHandle1 = GCHandle.FromIntPtr(ptr1);
                                string csString1 = (string)gCHandle1.Target;
                                string csString2 = (string)csString1.Clone();
                                GCHandle gCHandle2 = GCHandle.Alloc(csString2, GCHandleType.Weak);
                                IntPtr ptr2 = GCHandle.ToIntPtr(gCHandle2);
                                lua.PushUserType(ptr2, UserType_Id);
                                return 1;
                            });
                            break;
                        case "IndexOf":
                            lua.PushManagedFunction((lua) =>
                            {
                                IntPtr ptr = lua.GetUserType(1, UserType_Id);
                                GCHandle gCHandle = GCHandle.FromIntPtr(ptr);
                                string csString = (string)gCHandle.Target;

                                string toFind;
                                if (lua.IsType(2, TYPES.STRING))
                                {
                                    toFind = lua.GetString(2);
                                }
                                else if (lua.IsType(2, UserType_Id))
                                {
                                    IntPtr ptr2 = lua.GetUserType(2, UserType_Id);
                                    GCHandle gCHandle2 = GCHandle.FromIntPtr(ptr2);
                                    toFind = (string)gCHandle2.Target;
                                }
                                else return 0;

                                int indexOf = csString.IndexOf(toFind);
                                lua.PushNumber(indexOf);
                                return 1;
                            });
                            break;
                        case "Contains":
                            lua.PushManagedFunction((lua) =>
                            {
                                IntPtr ptr = lua.GetUserType(1, UserType_Id);
                                GCHandle gCHandle = GCHandle.FromIntPtr(ptr);
                                string csString = (string)gCHandle.Target;

                                string toFind;
                                if (lua.IsType(2, TYPES.STRING))
                                {
                                    toFind = lua.GetString(2);
                                }
                                else if (lua.IsType(2, UserType_Id))
                                {
                                    IntPtr ptr2 = lua.GetUserType(2, UserType_Id);
                                    GCHandle gCHandle2 = GCHandle.FromIntPtr(ptr2);
                                    toFind = (string)gCHandle2.Target;
                                }
                                else return 0;

                                bool contains = csString.Contains(toFind);
                                lua.PushBool(contains);
                                return 1;
                            });
                            break;
                        default:
                            lua.PushNil();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("nil passed to __index");
                    lua.PushNil();
                }
                // function will return 1 result
                return 1;
            });
            lua.SetField(-2, "__index");

            lua.PushManagedFunction((lua) =>
            {
                // 1 - stands for 1st passed to function argument
                IntPtr ptr = lua.GetUserType(1, UserType_Id);
                if (ptr != IntPtr.Zero)
                {
                    GCHandle gCHandle = GCHandle.FromIntPtr(ptr);
                    string csString = (string)gCHandle.Target;
                    lua.PushString(csString);
                }
                else
                {
                    Console.WriteLine("nil passed to __tostring");
                    lua.PushNil();
                }
                return 1;
            });
            lua.SetField(-2, "__tostring");

            // equal (==) method
            lua.PushManagedFunction((lua) =>
            {
                IntPtr ptr1 = lua.GetUserType(1, UserType_Id);
                IntPtr ptr2 = lua.GetUserType(2, UserType_Id);
                // if we have same pointers, then objects are same
                if (ptr1 == ptr2)
                {
                    lua.PushBool(true);
                }
                // check if both pointers not zero
                else if (ptr1 != IntPtr.Zero && ptr2 != IntPtr.Zero)
                {
                    GCHandle gCHandle1 = GCHandle.FromIntPtr(ptr1);
                    GCHandle gCHandle2 = GCHandle.FromIntPtr(ptr2);
                    string csString1 = (string)gCHandle1.Target;
                    string csString2 = (string)gCHandle2.Target;
                    lua.PushBool(csString1 == csString2);
                }
                // some of pointers is Zero, we'll not compare them
                else
                {
                    lua.PushBool(false);
                }
                return 1;
            });
            lua.SetField(-2, "__eq");

            // Dispose() in lua
            lua.PushManagedFunction((lua) =>
            {
                // 1 - stands for 1st passed to function argument
                IntPtr ptr = lua.GetUserType(1, UserType_Id);
                if (ptr != IntPtr.Zero)
                {
                    GCHandle gCHandle = GCHandle.FromIntPtr(ptr);
                    string csString = (string)gCHandle.Target;
                    Console.WriteLine($"csString ({csString}) is garbage collected");
                    gCHandle.Free();
                }
                else
                {
                    Console.WriteLine("nil passed to __gc");
                }
                return 0;
            });
            lua.SetField(-2, "__gc");
            lua.Pop();

            // now we need to somehow create it
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.PushManagedFunction((lua) =>
            {
                string csString = lua.GetString(1);
                GCHandle gCHandle = GCHandle.Alloc(csString, GCHandleType.Weak);
                IntPtr ptr = GCHandle.ToIntPtr(gCHandle);
                lua.PushUserType(ptr, UserType_Id);
                return 1;
            });
            lua.SetField(-2, "CreateCSString");
            lua.Pop();
        }

        public void Unload(ILua lua)
        {
            // clean our meta-methods
            lua.PushMetaTable(UserType_Id);
            // remove __index method
            lua.PushNil();
            lua.SetField(-2, "__index");
            // remove __eq method
            lua.PushNil();
            lua.SetField(-2, "__eq");
            // remove __gc method
            lua.PushNil();
            lua.SetField(-2, "__gc");
            // remove __tostring method
            lua.PushNil();
            lua.SetField(-2, "__tostring");
            lua.Pop();

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.PushNil();
            lua.SetField(-2, "CreateCSString");
            lua.Pop();
        }
    }
}
