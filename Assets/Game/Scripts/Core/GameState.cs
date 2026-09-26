using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SecretVirus
{
    public enum Hero { Daeun, James, Daniel }
    public enum Resolution { Lethal, Subdued, Persuaded, Peaceful, Avoided }
    public enum GameMode { Title, Opening, Field, Dialogue, Inventory, Crafting, PuzzleA, PuzzleB, Debate, Battle, Menu, Settings, Ending, Gallery }
    [Serializable] public class ItemStack { public string id; public int count; public ItemStack(string id, int count) { this.id = id; this.count = count; } }
    [Serializable] public class Incident { public string id; public Resolution result; public Incident(string id, Resolution result) { this.id = id; this.result = result; } }
    [Serializable] public class GameState
    {
        public int version = 1, stage = 1, highestStage = 1, tutorial, hp = 100, leader, joined = 1;
        public float x = 5, y = 11, playSeconds;
        public bool poison, offerAccepted, bossWon, bossLost;
        public string ending = "";
        public List<ItemStack> items = new List<ItemStack>();
        public List<string> flags = new List<string>();
        public List<string> clues = new List<string>();
        public List<string> fragments = new List<string>();
        public List<Incident> incidents = new List<Incident>();
        public bool Has(string flag) { return flags.Contains(flag); }
        public bool Flag(string flag) { if (Has(flag)) return false; flags.Add(flag); return true; }
        public void Clue(string id) { if (!clues.Contains(id)) clues.Add(id); }
        public void Fragment(string id) { if (!fragments.Contains(id)) fragments.Add(id); }
        public int Total { get { return incidents.Count(i => i.result != Resolution.Avoided); } }
        public int NonLethal { get { return incidents.Count(i => i.result != Resolution.Avoided && i.result != Resolution.Lethal); } }
        public bool Humane { get { return Total > 0 && NonLethal * 2 >= Total; } }
        public string Ratio { get { return Total == 0 ? "기록 없음" : NonLethal + " / " + Total + "  ·  " + (100f * NonLethal / Total).ToString("0.#") + "%"; } }
        public bool Record(string id, Resolution result)
        {
            if (incidents.Any(i => i.id == id)) return false;
            incidents.Add(new Incident(id, result)); return true;
        }
        public string DetermineEnding()
        {
            if (offerAccepted) return "B";
            if (bossLost) return "D";
            if (!bossWon) return "";
            return Humane && fragments.Count == 3 ? "A" : "C";
        }
        public GameState Clone() { return JsonUtility.FromJson<GameState>(JsonUtility.ToJson(this)); }
        public bool Valid()
        {
            return version == 1 && stage >= 1 && stage <= 14 && highestStage >= stage && highestStage <= 14 &&
                joined >= 1 && joined <= 3 && leader >= 0 && leader < joined && hp >= 0 && hp <= 100 &&
                tutorial >= 0 && tutorial <= 5 && !float.IsNaN(x) && !float.IsNaN(y) && !float.IsInfinity(x) && !float.IsInfinity(y) &&
                x >= 0 && x <= 40 && y >= 0 && y <= 30 && items != null && items.Count <= 20 &&
                items.All(i => i != null && Catalog.Items.ContainsKey(i.id) && i.count > 0 && i.count <= 20) &&
                flags != null && clues != null && fragments != null && fragments.Count <= 3 &&
                fragments.Distinct().Count() == fragments.Count && fragments.All(f => f == "A" || f == "B" || f == "C") &&
                incidents != null && incidents.All(i => i != null && !string.IsNullOrEmpty(i.id) && Enum.IsDefined(typeof(Resolution), i.result)) &&
                incidents.Select(i => i.id).Distinct().Count() == incidents.Count;
        }
    }
    public static class Inventory
    {
        public const int Capacity = 20, StackLimit = 20;
        public static int Count(GameState s, string id) { return s.items.Where(i => i.id == id).Sum(i => i.count); }
        public static bool Add(GameState s, string id, int count)
        {
            if (count <= 0 || !Catalog.Items.ContainsKey(id)) return false;
            int room = (Capacity - s.items.Count) * StackLimit + s.items.Where(i => i.id == id).Sum(i => StackLimit - i.count);
            if (room < count) return false;
            foreach (var stack in s.items.Where(i => i.id == id)) { int n = Math.Min(count, StackLimit - stack.count); stack.count += n; count -= n; }
            while (count > 0) { int n = Math.Min(count, StackLimit); s.items.Add(new ItemStack(id, n)); count -= n; }
            return true;
        }
        public static bool Take(GameState s, string id, int count)
        {
            if (count <= 0 || Count(s, id) < count) return false;
            foreach (var stack in s.items.Where(i => i.id == id).ToArray()) { int n = Math.Min(count, stack.count); stack.count -= n; count -= n; if (stack.count == 0) s.items.Remove(stack); }
            return true;
        }
    }
    public class Recipe
    {
        public string id, name, output; public string[] parts; public int[] counts;
        public Recipe(string id, string name, string output, string[] parts, int[] counts) { this.id = id; this.name = name; this.output = output; this.parts = parts; this.counts = counts; }
        public bool Ready(GameState s) { for (int i = 0; i < parts.Length; i++) if (Inventory.Count(s, parts[i]) < counts[i]) return false; return true; }
    }
    public class CraftSession
    {
        public Recipe recipe; public string[] slots = new string[5]; public int[] quantities = new int[5]; public int withdrawals;
        public CraftSession(Recipe r) { recipe = r; }
        public bool Place(GameState s, string item, int slot)
        {
            if (slot < 0 || slot >= 5 || slots[slot] != null) return false;
            int amount = item == recipe.parts[slot] ? recipe.counts[slot] : 1;
            int reserved = 0; for (int i = 0; i < 5; i++) if (slots[i] == item) reserved += quantities[i];
            if (Inventory.Count(s, item) - reserved < amount) return false;
            slots[slot] = item; quantities[slot] = amount; return true;
        }
        public bool Withdraw(int slot) { if (slot < 0 || slot >= 5 || slots[slot] == null) return false; slots[slot] = null; quantities[slot] = 0; withdrawals++; return true; }
        public bool Correct(int slot) { return slots[slot] == recipe.parts[slot] && quantities[slot] == recipe.counts[slot]; }
        public bool Complete { get { for (int i = 0; i < 5; i++) if (!Correct(i)) return false; return true; } }
        public bool Commit(GameState s)
        {
            if (!Complete || !recipe.Ready(s)) return false;
            var copy = s.Clone();
            for (int i = 0; i < 5; i++) if (!Inventory.Take(copy, recipe.parts[i], recipe.counts[i])) return false;
            if (!Inventory.Add(copy, recipe.output, 1)) return false;
            s.items = copy.items; slots = new string[5]; quantities = new int[5]; return true;
        }
    }
    [Serializable] public class Preferences
    {
        public float master = .7f, music = .45f, effects = .75f, textSpeed = 38, textScale = 1;
        public bool shake = true, flashes = true;
        public int interact = (int)KeyCode.Z, cancel = (int)KeyCode.X, switchHero = (int)KeyCode.C, inventory = (int)KeyCode.I;
        public List<string> endings = new List<string>();
    }
    [Serializable] class SaveEnvelope { public string payload, checksum; public int format = 1; }
    public static class SaveStore
    {
        public static string DirectoryPath { get { return Path.Combine(Application.persistentDataPath, "Campaign"); } }
        public static string SlotPath(string slot) { return Path.Combine(DirectoryPath, slot + ".json"); }
        static string Hash(string value) { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value))); }
        public static bool Write(GameState state, string slot, out string error)
        {
            error = "";
            if (state.tutorial < 5 || !state.Valid()) { error = "지금은 저장할 수 없습니다."; return false; }
            try {
                Directory.CreateDirectory(DirectoryPath);
                string payload = JsonUtility.ToJson(state), path = SlotPath(slot), temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(new SaveEnvelope { payload = payload, checksum = Hash(payload) }), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
                return true;
            } catch (Exception e) { error = "저장 실패. 마지막 정상 저장은 유지됩니다. " + e.Message; return false; }
        }
        public static GameState Read(string slot, out string error)
        {
            error = ""; string path = SlotPath(slot);
            foreach (string candidate in new[] { path, path + ".bak" }) {
                try {
                    if (!File.Exists(candidate)) continue;
                    var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(candidate, Encoding.UTF8));
                    if (envelope == null || envelope.format != 1 || envelope.payload == null || envelope.checksum != Hash(envelope.payload)) continue;
                    var state = JsonUtility.FromJson<GameState>(envelope.payload);
                    if (state != null && state.Valid()) { if (candidate.EndsWith(".bak")) error = "백업 저장을 복구했습니다."; return state; }
                } catch (Exception) { }
            }
            error = "사용 가능한 저장이 없습니다."; return null;
        }
        public static Preferences ReadPreferences()
        {
            try { var p = JsonUtility.FromJson<Preferences>(File.ReadAllText(SlotPath("preferences"))); if (p != null) { if (p.endings == null) p.endings = new List<string>(); p.master = Mathf.Clamp01(p.master); p.music = Mathf.Clamp01(p.music); p.effects = Mathf.Clamp01(p.effects); p.textScale = Mathf.Clamp(p.textScale,.85f,1.2f); p.textSpeed = Mathf.Clamp(p.textSpeed,10,100); return p; } } catch (Exception) { }
            return new Preferences();
        }
        public static bool WritePreferences(Preferences p)
        {
            try { Directory.CreateDirectory(DirectoryPath); string path = SlotPath("preferences"), temp = path + ".tmp"; File.WriteAllText(temp,JsonUtility.ToJson(p),new UTF8Encoding(false)); if (File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path); return true; } catch (Exception) { return false; }
        }
    }
}
