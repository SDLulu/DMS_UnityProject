import Phaser from "phaser";
import "./style.css";

const WIDTH = 1280;
const HEIGHT = 720;
const FLOOR_Y = 596;

type ReelId = "title" | "dive" | "combat" | "core" | "failure" | "home" | "ending";

interface Cue {
  at: number;
  speaker: string;
  text: string;
  duration: number;
}

interface ReelScene {
  id: ReelId;
  title: string;
  check: string;
  duration: number;
  cues: Cue[];
}

class VisualReelScene extends Phaser.Scene {
  private keys!: Record<string, Phaser.Input.Keyboard.Key>;
  private sceneIndex = 0;
  private elapsed = 0;
  private activeCue: Cue | null = null;
  private glitch = 0;
  private flash = 0;
  private shake = 0;
  private autoPlay = true;
  private scanOffset = 0;
  private lastStateText = "";

  private readonly scenes: ReelScene[] = [
    {
      id: "title",
      title: "01 TITLE MOOD",
      check: "먼 도시와 뒷골목 대비, 주인공 실루엣, 타이틀 글로우",
      duration: 8,
      cues: [
        { at: 1.0, speaker: "SYSTEM", text: "[기억층 접속 대기]", duration: 1.4 },
        { at: 4.2, speaker: "브로커", text: "파일 하나만 꺼내. 내용은 보지 말고.", duration: 2.0 },
      ],
    },
    {
      id: "dive",
      title: "02 DEEP DIVE START",
      check: "접속 텍스트, 낙하감, 데이터 터널, 글리치 강도",
      duration: 9,
      cues: [
        { at: 0.4, speaker: "SYSTEM", text: "[접속 중...] [대상: 채무자 047] [회수 파일: HOME]", duration: 2.0 },
        { at: 2.8, speaker: "주인공", text: "잡음 심해.", duration: 1.2 },
        { at: 5.2, speaker: "브로커", text: "경로는 짧다. 멈추지 마.", duration: 1.6 },
      ],
    },
    {
      id: "combat",
      title: "03 ACTION SLICE",
      check: "대시 잔상, 공격 궤적, 히트스톱 느낌, 적 가시성",
      duration: 8,
      cues: [
        { at: 0.8, speaker: "브로커", text: "감시 개체 셋.", duration: 1.3 },
        { at: 2.2, speaker: "주인공", text: "숫자 세지 마. 느려져.", duration: 1.6 },
        { at: 5.4, speaker: "주인공", text: "다음.", duration: 0.9 },
      ],
    },
    {
      id: "core",
      title: "04 HOME CORE",
      check: "HOME이 아이템이 아니라 기억처럼 보이는지, 따뜻함과 위험함의 비율",
      duration: 10,
      cues: [
        { at: 1.0, speaker: "SYSTEM", text: "[회수 대상 확인] 파일명: HOME / 보호 등급: 비정상적으로 높음", duration: 2.1 },
        { at: 3.9, speaker: "아이", text: "오늘부터 여기가 우리 집이야?", duration: 1.8 },
        { at: 6.1, speaker: "아버지", text: "그래. 작지만 우리 거야.", duration: 1.9 },
      ],
    },
    {
      id: "failure",
      title: "05 EXTRACTION FAILURE",
      check: "잔류 인격 반응, 화면 붕괴, 강제 복귀 충격",
      duration: 7,
      cues: [
        { at: 0.6, speaker: "SYSTEM", text: "[심층 잠금 발생] [잔류 인격 반응 감지]", duration: 1.8 },
        { at: 2.7, speaker: "잔류 인격", text: "그건 팔 물건이 아니야.", duration: 1.8 },
        { at: 5.0, speaker: "SYSTEM", text: "[강제 복귀]", duration: 1.0 },
      ],
    },
    {
      id: "home",
      title: "06 PLAYER HOME",
      check: "현실의 차가움, 빚 UI, 단말기 빛, 방의 쓸쓸함",
      duration: 9,
      cues: [
        { at: 0.9, speaker: "SYSTEM", text: "[회수 실패] [파일 일부만 확보] [채무 패널티 예정]", duration: 2.0 },
        { at: 3.4, speaker: "주인공", text: "튕겼네.", duration: 1.1 },
        { at: 5.4, speaker: "단말기", text: "[채무 잔액] 83,420C  /  원본 칩에 직접 접속해야 한다.", duration: 2.4 },
      ],
    },
    {
      id: "ending",
      title: "07 ENDING DOOR",
      check: "미전송 1KB, 문을 살짝 열어두는 여운, 조용한 엔딩",
      duration: 10,
      cues: [
        { at: 1.0, speaker: "브로커", text: "전송 누락은 없겠지?", duration: 1.8 },
        { at: 3.4, speaker: "주인공", text: "없어.", duration: 1.1 },
        { at: 5.5, speaker: "SYSTEM", text: "[미전송 데이터: 1KB] voice_001: \"아빠, 문 열어둘게.\"", duration: 3.0 },
      ],
    },
  ];

  preload() {
    this.load.image("bg-title-alley", "/assets/generated/bg_title_alley.png");
    this.load.image("bg-memory-home", "/assets/generated/bg_memory_home.png");
    this.load.image("bg-player-room", "/assets/generated/bg_player_room.png");
    this.load.image("spr-residual-guardian", "/assets/generated/spr_residual_guardian.png");
    for (let i = 1; i <= 5; i++) this.load.image(`cp-bg-${i}`, `/assets/craftpix/bg8_night_${i}.png`);
    this.load.image("cp-market-tile-1", "/assets/craftpix/market_tile_01.png");
    this.load.image("cp-market-tile-2", "/assets/craftpix/market_tile_02.png");
    this.load.image("cp-billboard", "/assets/craftpix/billboard_128x64.png");
    this.load.image("cp-billboard-pillar", "/assets/craftpix/billboard_pillar.png");
    this.load.spritesheet("cp-player-idle", "/assets/craftpix/player_idle.png", { frameWidth: 48, frameHeight: 48 });
    this.load.spritesheet("cp-player-walk", "/assets/craftpix/player_walk.png", { frameWidth: 48, frameHeight: 48 });
    this.load.spritesheet("cp-enemy-idle", "/assets/craftpix/enemy_idle.png", { frameWidth: 48, frameHeight: 48 });
    this.load.spritesheet("cp-enemy-attack", "/assets/craftpix/enemy_attack.png", { frameWidth: 48, frameHeight: 48 });
  }

  create() {
    this.cameras.main.setBackgroundColor("#03050a");
    this.keys = this.input.keyboard!.addKeys({
      one: Phaser.Input.Keyboard.KeyCodes.ONE,
      two: Phaser.Input.Keyboard.KeyCodes.TWO,
      three: Phaser.Input.Keyboard.KeyCodes.THREE,
      four: Phaser.Input.Keyboard.KeyCodes.FOUR,
      five: Phaser.Input.Keyboard.KeyCodes.FIVE,
      six: Phaser.Input.Keyboard.KeyCodes.SIX,
      seven: Phaser.Input.Keyboard.KeyCodes.SEVEN,
      left: Phaser.Input.Keyboard.KeyCodes.LEFT,
      right: Phaser.Input.Keyboard.KeyCodes.RIGHT,
      space: Phaser.Input.Keyboard.KeyCodes.SPACE,
      f: Phaser.Input.Keyboard.KeyCodes.F,
    }) as Record<string, Phaser.Input.Keyboard.Key>;
    this.installTestHooks();
  }

  update(_time: number, deltaMs: number) {
    const dt = Math.min(deltaMs / 1000, 1 / 30);
    this.handleInput();
    this.elapsed += dt;
    this.scanOffset += dt * 34;
    this.glitch = Math.max(0, this.glitch - dt);
    this.flash = Math.max(0, this.flash - dt);
    this.shake = Math.max(0, this.shake - dt);

    const current = this.current();
    if (this.autoPlay && this.elapsed >= current.duration) {
      this.gotoScene((this.sceneIndex + 1) % this.scenes.length);
    }

    this.updateCue();
    this.renderCurrent();
    this.updateDom();
    this.lastStateText = this.computeStateText();
  }

  private handleInput() {
    const numberKeys = ["one", "two", "three", "four", "five", "six", "seven"];
    numberKeys.forEach((key, index) => {
      if (Phaser.Input.Keyboard.JustDown(this.keys[key])) this.gotoScene(index);
    });
    if (Phaser.Input.Keyboard.JustDown(this.keys.right)) this.gotoScene((this.sceneIndex + 1) % this.scenes.length);
    if (Phaser.Input.Keyboard.JustDown(this.keys.left)) this.gotoScene((this.sceneIndex + this.scenes.length - 1) % this.scenes.length);
    if (Phaser.Input.Keyboard.JustDown(this.keys.space)) this.autoPlay = !this.autoPlay;
    if (Phaser.Input.Keyboard.JustDown(this.keys.f)) {
      if (this.scale.isFullscreen) this.scale.stopFullscreen();
      else this.scale.startFullscreen();
    }
  }

  private gotoScene(index: number) {
    this.sceneIndex = Phaser.Math.Clamp(index, 0, this.scenes.length - 1);
    this.elapsed = 0;
    this.activeCue = null;
    this.glitch = 0.4;
    this.flash = 0.18;
    this.shake = 0.18;
  }

  private current() {
    return this.scenes[this.sceneIndex];
  }

  private updateCue() {
    const cue = this.current().cues.find((item) => this.elapsed >= item.at && this.elapsed <= item.at + item.duration);
    this.activeCue = cue ?? null;
  }

  private renderCurrent() {
    this.children.removeAll(true);
    const g = this.add.graphics();
    const scene = this.current();
    const p = Phaser.Math.Clamp(this.elapsed / scene.duration, 0, 1);
    const shakeX = this.shake > 0 ? Math.sin(this.elapsed * 95) * 5 * this.shake : 0;
    const shakeY = this.shake > 0 ? Math.cos(this.elapsed * 83) * 3 * this.shake : 0;
    g.save();
    g.translateCanvas(shakeX, shakeY);

    if (scene.id === "title") this.drawTitle(g, p);
    if (scene.id === "dive") this.drawDive(g, p);
    if (scene.id === "combat") this.drawCombat(g, p);
    if (scene.id === "core") this.drawCore(g, p);
    if (scene.id === "failure") this.drawFailure(g, p);
    if (scene.id === "home") this.drawHome(g, p, false);
    if (scene.id === "ending") this.drawHome(g, p, true);

    g.restore();
    this.drawOverlay(g);
    this.drawReelText(scene);
  }

  private drawTitle(g: Phaser.GameObjects.Graphics, p: number) {
    this.drawPixelCityStage(g, "alley", p);
    this.drawRain(g, 0.5);
    this.drawPixelPlayer(g, 602, FLOOR_Y - 96, 1.2, 1);
    this.drawPixelTitle(g);
  }

  private drawDive(g: Phaser.GameObjects.Graphics, p: number) {
    this.drawPixelMemoryStage(g, p, 0.72);
    const cx = WIDTH / 2;
    const cy = HEIGHT / 2;
    for (let i = 0; i < 42; i++) {
      const depth = (i / 42 + p * 1.8) % 1;
      const size = 40 + depth * 980;
      const alpha = (1 - depth) * 0.28;
      g.lineStyle(2, i % 3 === 0 ? 0xff477d : 0x61f4ff, alpha);
      g.strokeRect(cx - size * 0.7, cy - size * 0.42, size * 1.4, size * 0.84);
    }
    for (let i = 0; i < 80; i++) {
      const y = (i * 47 + p * 1300) % HEIGHT;
      const x = (i * 193) % WIDTH;
      g.fillStyle(i % 4 === 0 ? 0xff477d : 0x84fff6, 0.16).fillRect(x, y, 3 + (i % 6) * 8, 2);
    }
    this.drawPixelPlayer(g, cx - 22 + Math.sin(p * 18) * 10, 260 + p * 250, 0.9, 0.95);
    this.drawSystemPanel(g, 380, 92, "[접속 중...]\n대상: 채무자 047\n회수 파일: HOME\n침입 경로 확보", 0.78);
    if (p > 0.72) this.glitch = Math.max(this.glitch, 0.08);
  }

  private drawCombat(g: Phaser.GameObjects.Graphics, p: number) {
    this.drawPixelMemoryStage(g, p, 0.5);
    this.drawPixelPlatforms(g, "combat");
    const dash = Phaser.Math.Easing.Cubic.Out(Math.min(1, p * 1.7));
    const x = 130 + dash * 760;
    for (let i = 0; i < 8; i++) {
      g.fillStyle(0x67f4ff, 0.08 * (8 - i)).fillRect(x - i * 38, FLOOR_Y - 74, 42, 54);
    }
    this.drawSpeedLines(g, x, FLOOR_Y - 126, p);
    this.drawPixelPlayer(g, x, FLOOR_Y - 104, 1, 1);
    const slashAlpha = p > 0.28 && p < 0.58 ? 1 : 0.22;
    this.drawSlash(g, x + 44, FLOOR_Y - 94, slashAlpha);
    if (p > 0.36 && p < 0.54) this.drawHitBurst(g, 600, FLOOR_Y - 92, 1 - Math.abs(p - 0.45) * 5);
    this.drawPixelEnemy(g, 600, FLOOR_Y - 58, p > 0.45 ? 0.3 : 1);
    this.drawPixelEnemy(g, 790, FLOOR_Y - 58, p > 0.62 ? 0.18 : 1);
    this.drawPixelDrone(g, 960, 390, p > 0.72 ? 0.2 : 1);
    if (p > 0.44 && p < 0.5) this.flash = Math.max(this.flash, 0.12);
    if (p > 0.6 && p < 0.66) this.shake = Math.max(this.shake, 0.16);
  }

  private drawCore(g: Phaser.GameObjects.Graphics, p: number) {
    this.drawPixelMemoryStage(g, p, 0.85);
    this.drawPixelPlatforms(g, "core");
    const cx = WIDTH / 2;
    const cy = 364;
    for (let i = 0; i < 7; i++) {
      const r = 56 + i * 34 + Math.sin(this.elapsed * 2.3 + i) * 8;
      g.lineStyle(2, i % 2 ? 0xffd66b : 0x79fff7, 0.18 - i * 0.014);
      g.strokeCircle(cx, cy, r);
    }
    g.fillStyle(0x79fff7, 0.22).fillCircle(cx, cy, 118);
    g.fillStyle(0xffd66b, 0.28).fillCircle(cx, cy, 78);
    g.fillStyle(0xffffff, 0.96).fillCircle(cx, cy, 20);
    g.lineStyle(3, 0xff477d, 0.7).strokeCircle(cx, cy, 142 + Math.sin(this.elapsed * 5) * 8);
    this.drawPixelPlayer(g, 424 + Math.min(1, p * 1.4) * 120, FLOOR_Y - 104, 0.95, 1);
  }

  private drawFailure(g: Phaser.GameObjects.Graphics, p: number) {
    this.drawPixelMemoryStage(g, p, 0.7);
    g.fillStyle(0x000000, 0.15 + p * 0.32).fillRect(0, 0, WIDTH, HEIGHT);
    const cx = WIDTH / 2;
    const cy = HEIGHT / 2;
    for (let i = 0; i < 24; i++) {
      const angle = (i / 24) * Math.PI * 2 + this.elapsed * 0.7;
      const len = 80 + i * 17 + p * 520;
      g.lineStyle(2, i % 2 ? 0xff477d : 0x71fff5, 0.35);
      g.lineBetween(cx, cy, cx + Math.cos(angle) * len, cy + Math.sin(angle) * len);
    }
    this.drawPixelResidualBoss(g, cx, 382, 1.25, 0.94);
    this.drawSystemPanel(g, 410, 448, "[심층 잠금 발생]\n잔류 인격 반응 감지\n접속 불안정\n강제 복귀 준비", 0.86);
    this.glitch = Math.max(this.glitch, 0.25 + Math.sin(this.elapsed * 13) * 0.08);
    if (p > 0.74) this.flash = Math.max(this.flash, 0.25);
  }

  private drawHome(g: Phaser.GameObjects.Graphics, p: number, ending: boolean) {
    this.drawPixelApartment(g, p, ending);
    this.drawPixelTerminal(g, 812, 428, 0.9 + Math.sin(this.elapsed * 4) * 0.08);
    const doorOpen = ending ? Phaser.Math.SmoothStep(p, 0.35, 0.78) : 0;
    this.drawPixelDoor(g, 1060, 286, doorOpen);
    this.drawPixelPlayer(g, ending ? 742 : 756, FLOOR_Y - 104, 0.95, 1);
    if (ending) {
      g.fillStyle(0xffd66b, 0.18 * doorOpen).fillTriangle(1002, 300, 1160, FLOOR_Y, 1002, FLOOR_Y);
      this.drawSmallNeon(g, 452, 116, "[미전송 데이터: 1KB]", 0xffe59a);
    } else {
      this.drawDebtPanel(g, p);
    }
  }

  private drawNeonCity(g: Phaser.GameObjects.Graphics, alpha: number) {
    g.fillStyle(0x06101a, 1).fillRect(0, 0, WIDTH, HEIGHT);
    const colors = [0x0f3349, 0x121a2b, 0x0b4350, 0x28172f, 0x1b2538];
    for (let i = 0; i < 20; i++) {
      const w = 58 + (i % 4) * 18;
      const h = 130 + ((i * 59) % 300);
      const x = i * 72 - 38;
      g.fillStyle(colors[i % colors.length], alpha).fillRect(x, FLOOR_Y - h, w, h);
      for (let y = FLOOR_Y - h + 26; y < FLOOR_Y - 20; y += 46) {
        g.fillStyle(i % 3 === 0 ? 0xff4a88 : 0x65f4ff, 0.35).fillRect(x + 10, y, w * 0.44, 5);
      }
    }
  }

  private drawPixelCityStage(g: Phaser.GameObjects.Graphics, variant: "alley", p: number) {
    this.drawCraftpixParallax(p, 1);
    g.fillStyle(0x040711, 0.16).fillRect(0, 0, WIDTH, HEIGHT);
    this.drawCraftpixGround(0, FLOOR_Y - 64, 1);

    this.drawForegroundBuilding(g, -18, 220, 210, 376, 0x070910, 0x1cd8ff);
    this.drawForegroundBuilding(g, 1026, 170, 282, 426, 0x070910, 0xff3f91);
    this.drawHangingCables(g);

    this.drawCraftpixBillboard(g, 126, 292, 1.5, "MEMORY");
    this.drawCraftpixBillboard(g, 918, 420, 1.25, "CREDIT");
    this.drawCraftpixBillboard(g, 1028, 286, 1.2, "HOME");
    this.drawSilhouetteForeground(g, 0.95);
  }

  private drawPixelSkyline(
    g: Phaser.GameObjects.Graphics,
    offsetX: number,
    baseY: number,
    alpha: number,
    buildingColor: number,
    lightColor: number,
  ) {
    for (let i = 0; i < 22; i++) {
      const x = offsetX + i * 68;
      const w = 44 + (i % 4) * 14;
      const h = 96 + ((i * 47) % 230);
      g.fillStyle(buildingColor, alpha).fillRect(x, baseY - h, w, h);
      for (let yy = baseY - h + 18; yy < baseY - 20; yy += 28) {
        if ((i + yy) % 3 === 0) continue;
        g.fillStyle(lightColor, alpha * 0.48).fillRect(x + 8, yy, 18 + (i % 3) * 8, 4);
      }
    }
  }

  private drawForegroundBuilding(
    g: Phaser.GameObjects.Graphics,
    x: number,
    y: number,
    w: number,
    h: number,
    color: number,
    accent: number,
  ) {
    g.fillStyle(color, 0.98).fillRect(x, y, w, h);
    g.fillStyle(0x101522, 1).fillRect(x + 18, y + 28, w - 36, h - 48);
    for (let yy = y + 44; yy < y + h - 20; yy += 38) {
      g.fillStyle(accent, 0.42).fillRect(x + 34, yy, w - 72, 5);
    }
    g.lineStyle(3, 0x000000, 0.55).strokeRect(x, y, w, h);
  }

  private drawHangingCables(g: Phaser.GameObjects.Graphics) {
    g.lineStyle(4, 0x02030a, 0.88);
    for (let i = 0; i < 5; i++) {
      const y = 145 + i * 38;
      g.beginPath();
      g.moveTo(180, y);
      g.lineTo(380, y + 34 + i * 5);
      g.lineTo(680, y + 14);
      g.lineTo(1040, y + 42);
      g.strokePath();
    }
  }

  private drawSilhouetteForeground(g: Phaser.GameObjects.Graphics, alpha: number) {
    g.fillStyle(0x02030a, 0.78 * alpha).fillRect(0, FLOOR_Y + 12, WIDTH, HEIGHT - FLOOR_Y);
    g.fillStyle(0x02030a, 0.92 * alpha).fillRect(0, FLOOR_Y - 76, 86, 108);
    g.fillStyle(0x02030a, 0.92 * alpha).fillRect(1194, FLOOR_Y - 90, 86, 122);
    for (let x = 18; x < WIDTH; x += 118) {
      g.fillStyle(0x02030a, 0.58 * alpha).fillRect(x, FLOOR_Y - 8, 42, 18);
    }
    g.lineStyle(5, 0x02030a, 0.9 * alpha);
    g.lineBetween(0, FLOOR_Y - 3, WIDTH, FLOOR_Y - 3);
  }

  private drawPixelSign(g: Phaser.GameObjects.Graphics, x: number, y: number, w: number, h: number, color: number) {
    g.fillStyle(0x04070d, 0.84).fillRect(x, y, w, h);
    g.lineStyle(3, color, 0.85).strokeRect(x, y, w, h);
    for (let i = 0; i < 5; i++) {
      g.fillStyle(color, 0.62).fillRect(x + 12 + i * 20, y + 12, 10, 4);
    }
  }

  private drawPixelTitle(g: Phaser.GameObjects.Graphics) {
    g.fillStyle(0x02040b, 0.84).fillRect(406, 182, 468, 98);
    g.lineStyle(4, 0x1cd8ff, 0.9).strokeRect(406, 182, 468, 98);
    const title = this.add.text(640, 224, "DEEP DIVE: HOME", {
      fontFamily: "Consolas, 'Segoe UI', monospace",
      fontSize: "44px",
      color: "#e9fbff",
      fontStyle: "700",
    }).setOrigin(0.5);
    title.setShadow(0, 0, "#1cd8ff", 14, true, true);
    this.add.text(640, 314, "1-7 REVIEW SCENES", {
      fontFamily: "Consolas, monospace",
      fontSize: "20px",
      color: "#9fffee",
    }).setOrigin(0.5).setShadow(0, 0, "#1cd8ff", 8, true, true);
  }

  private drawPixelMemoryStage(g: Phaser.GameObjects.Graphics, p: number, warmth: number) {
    this.drawCraftpixParallax(p, 0.45);
    g.fillStyle(0x041522, 0.72).fillRect(0, 0, WIDTH, HEIGHT);
    for (let i = 0; i < 80; i++) {
      const x = (i * 97 + this.elapsed * 20) % WIDTH;
      const y = 70 + ((i * 43) % 430);
      const color = i % 4 === 0 ? 0xffb84d : 0x18d8e8;
      g.fillStyle(color, 0.05 + warmth * 0.05).fillRect(x, y, 4 + (i % 8) * 12, 3);
    }
    this.drawPixelMemoryRoom(g, 70, 288, 250, 190, 0xffb84d, warmth * 0.42);
    this.drawPixelMemoryRoom(g, 790, 202, 300, 210, 0xffb84d, warmth * 0.34);
    this.drawPixelMemoryTable(g, 378, 428, warmth * 0.48);
    this.drawCraftpixGround(0, FLOOR_Y - 64, 0.78);
  }

  private drawPixelMemoryRoom(
    g: Phaser.GameObjects.Graphics,
    x: number,
    y: number,
    w: number,
    h: number,
    color: number,
    alpha: number,
  ) {
    g.fillStyle(color, alpha * 0.12).fillRect(x, y, w, h);
    g.lineStyle(3, color, alpha).strokeRect(x, y, w, h);
    g.fillStyle(color, alpha * 0.7).fillRect(x + 24, y + 42, 54, 96);
    g.fillStyle(color, alpha * 0.52).fillRect(x + w - 88, y + 40, 52, 48);
  }

  private drawPixelMemoryTable(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    g.fillStyle(0xffb84d, alpha).fillRect(x, y, 172, 16);
    g.fillStyle(0xffb84d, alpha * 0.82).fillRect(x + 18, y + 16, 10, 64);
    g.fillStyle(0xffb84d, alpha * 0.82).fillRect(x + 142, y + 16, 10, 64);
  }

  private drawPixelPlatforms(g: Phaser.GameObjects.Graphics, variant: "combat" | "core") {
    this.drawCraftpixGround(0, FLOOR_Y - 64, 1);
    if (variant === "combat") {
      this.drawCraftpixGround(514, 456, 0.9, 6);
    }
  }

  private drawPixelPlayer(g: Phaser.GameObjects.Graphics, x: number, y: number, scale: number, alpha: number) {
    const frame = Math.floor(this.elapsed * 8) % 6;
    const sprite = this.add.sprite(x, y + 62 * scale, "cp-player-idle", frame);
    sprite.setOrigin(0.5, 1);
    sprite.setScale(2.15 * scale);
    sprite.setAlpha(alpha);
    sprite.setTint(0xd7efff);
    sprite.setDepth(5);
    const shadow = this.add.sprite(x + 3, y + 62 * scale + 2, "cp-player-idle", frame);
    shadow.setOrigin(0.5, 1);
    shadow.setScale(2.15 * scale);
    shadow.setAlpha(alpha * 0.38);
    shadow.setTint(0x03050a);
    shadow.setDepth(4);
    return;
    const s = 4 * scale;
    g.fillStyle(0x03050a, alpha).fillRect(x - 5 * s, y + 5 * s, 10 * s, 17 * s);
    g.fillStyle(0x0a1020, alpha).fillRect(x - 3 * s, y, 7 * s, 6 * s);
    g.fillStyle(0xd9e7ff, alpha * 0.95).fillRect(x - 4 * s, y + 7 * s, 8 * s, 12 * s);
    g.fillStyle(0x161f2e, alpha).fillRect(x - 3 * s, y + 9 * s, 6 * s, 9 * s);
    g.fillStyle(0x1cd8ff, alpha).fillRect(x + 3 * s, y + 2 * s, 2 * s, s);
    g.fillStyle(0x03050a, alpha).fillRect(x - 4 * s, y + 22 * s, 3 * s, 6 * s);
    g.fillStyle(0x03050a, alpha).fillRect(x + 1 * s, y + 22 * s, 3 * s, 6 * s);
  }

  private drawPixelEnemy(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    const frame = Math.floor(this.elapsed * 7) % 5;
    const sprite = this.add.sprite(x, y, "cp-enemy-idle", frame);
    sprite.setOrigin(0.5, 1);
    sprite.setScale(2.2);
    sprite.setAlpha(alpha);
    sprite.setDepth(5);
    return;
    g.fillStyle(0x05050a, alpha).fillRect(x - 22, y - 48, 44, 48);
    g.fillStyle(0xff3f91, alpha).fillRect(x - 16, y - 58, 32, 14);
    g.fillStyle(0xff3f91, alpha * 0.74).fillRect(x - 28, y - 8, 56, 8);
    g.lineStyle(3, 0xff3f91, alpha * 0.62).strokeRect(x - 26, y - 62, 52, 62);
  }

  private drawPixelDrone(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    g.fillStyle(0x05050a, alpha).fillRect(x - 26, y - 16, 52, 32);
    g.fillStyle(0x1cd8ff, alpha).fillRect(x - 14, y - 8, 28, 16);
    g.fillStyle(0xff3f91, alpha).fillRect(x - 4, y - 2, 8, 4);
    g.lineStyle(2, 0xff3f91, alpha * 0.8).lineBetween(x, y, x - 230, FLOOR_Y - 90);
  }

  private drawCraftpixParallax(p: number, alpha: number) {
    for (let i = 1; i <= 5; i++) {
      const layer = this.add.image(WIDTH / 2 - p * i * 12, HEIGHT / 2, `cp-bg-${i}`);
      layer.setScale(2.23);
      layer.setAlpha(alpha * (i === 1 ? 1 : 0.95));
      layer.setDepth(-30 + i);
    }
  }

  private drawCraftpixGround(x: number, y: number, alpha: number, tiles = 42) {
    for (let i = 0; i < tiles; i++) {
      const img = this.add.image(x + i * 32, y, i % 4 === 0 ? "cp-market-tile-2" : "cp-market-tile-1");
      img.setOrigin(0, 0);
      img.setScale(2);
      img.setAlpha(alpha);
      img.setDepth(1);
    }
  }

  private drawCraftpixBillboard(g: Phaser.GameObjects.Graphics, x: number, y: number, scale: number, label: string) {
    const pillar = this.add.image(x + 66 * scale, y + 76 * scale, "cp-billboard-pillar");
    pillar.setOrigin(0.5, 0);
    pillar.setScale(scale);
    pillar.setDepth(2);
    const board = this.add.image(x, y, "cp-billboard");
    board.setOrigin(0, 0);
    board.setScale(scale);
    board.setDepth(3);
    board.setAlpha(0.92 + Math.sin(this.elapsed * 5) * 0.05);

    const bw = 128 * scale;
    const bh = 64 * scale;
    const pulse = 0.55 + Math.sin(this.elapsed * 6 + x * 0.01) * 0.18;
    g.fillStyle(0x02040a, 0.38).fillRect(x + 8 * scale, y + 8 * scale, bw - 16 * scale, bh - 16 * scale);
    g.lineStyle(Math.max(2, 2 * scale), label === "HOME" ? 0xffc75c : 0x1cd8ff, pulse).strokeRect(
      x + 10 * scale,
      y + 10 * scale,
      bw - 20 * scale,
      bh - 20 * scale,
    );
    for (let i = 0; i < 5; i++) {
      const color = i % 2 === 0 ? 0xff3f91 : 0x9fffee;
      g.fillStyle(color, 0.32 + pulse * 0.22).fillRect(
        x + (16 + i * 18) * scale,
        y + (42 + (i % 2) * 4) * scale,
        (10 + i * 3) * scale,
        4 * scale,
      );
    }

    this.add.text(x + 18 * scale, y + 16 * scale, label, {
      fontFamily: "Consolas, monospace",
      fontSize: `${Math.max(10, 12 * scale)}px`,
      color: label === "HOME" ? "#ffe7a6" : "#9fffee",
    }).setDepth(4).setShadow(0, 0, "#1cd8ff", 8, true, true);
  }

  private drawPixelResidualBoss(g: Phaser.GameObjects.Graphics, x: number, y: number, scale: number, alpha: number) {
    const s = 4 * scale;
    g.fillStyle(0x06101a, alpha * 0.64).fillRect(x - 9 * s, y - 18 * s, 18 * s, 38 * s);
    g.fillStyle(0xbefcff, alpha * 0.82).fillRect(x - 6 * s, y - 12 * s, 12 * s, 26 * s);
    g.fillStyle(0x07101a, alpha).fillRect(x - 4 * s, y - 15 * s, 8 * s, 7 * s);
    g.fillStyle(0xffb84d, alpha).fillRect(x - 5 * s, y - 1 * s, 10 * s, 6 * s);
    g.fillStyle(0x1cd8ff, alpha).fillRect(x - 15 * s, y - 9 * s, 7 * s, 4 * s);
    g.fillStyle(0x1cd8ff, alpha).fillRect(x + 8 * s, y - 9 * s, 10 * s, 4 * s);
    g.lineStyle(3, 0x1cd8ff, alpha * 0.68).strokeRect(x - 12 * s, y - 19 * s, 24 * s, 42 * s);
    for (let i = 0; i < 18; i++) {
      const ox = Math.sin(i * 17) * 80;
      const oy = Math.cos(i * 31) * 140;
      g.fillStyle(i % 2 ? 0xff3f91 : 0x1cd8ff, alpha * 0.55).fillRect(x + ox, y + oy, 18 + (i % 3) * 12, 4);
    }
  }

  private drawPixelApartment(g: Phaser.GameObjects.Graphics, p: number, ending: boolean) {
    g.fillStyle(0x03050a, 1).fillRect(0, 0, WIDTH, HEIGHT);
    g.fillStyle(0x111722, 1).fillRect(92, 130, 1098, 466);
    g.fillStyle(0x1a2230, 1).fillRect(118, 160, 1046, 390);
    for (let x = 118; x < 1164; x += 48) {
      g.fillStyle(0x0f1622, 0.45).fillRect(x, 160, 2, 390);
    }
    g.fillStyle(0x05070b, 1).fillRect(174, 228, 220, 154);
    g.lineStyle(4, 0x1c3344, 1).strokeRect(174, 228, 220, 154);
    g.fillStyle(0x11344b, 0.9).fillRect(188, 244, 192, 124);
    g.fillStyle(0x2bb8ff, 0.35).fillRect(206, 268, 34, 70);
    g.fillStyle(0xff3f91, 0.35).fillRect(318, 256, 38, 86);
    g.fillStyle(0x090c12, 1).fillRect(254, 482, 210, 38);
    g.fillStyle(0x0d111a, 1).fillRect(288, 412, 112, 72);
    g.fillStyle(0x080a10, 1).fillRect(1040, 282, 112, 314);
    g.fillStyle(0x121c28, 1).fillRect(1068, 308, 62, 288);
    g.fillStyle(0x0a0d13, 1).fillRect(0, FLOOR_Y, WIDTH, HEIGHT - FLOOR_Y);
    g.lineStyle(3, 0x182536, 1).lineBetween(0, FLOOR_Y, WIDTH, FLOOR_Y);
    if (!ending && p > 0.4) {
      g.fillStyle(0x02040a, 0.74).fillRect(92, 70, 500, 166);
      g.lineStyle(2, 0x1cd8ff, 0.42).strokeRect(92, 70, 500, 166);
      this.add.text(118, 94, "[회수 실패]\n파일 일부만 확보", {
        fontFamily: "Consolas, monospace",
        fontSize: "24px",
        lineSpacing: 10,
        color: "#e7fbff",
      });
    }
  }

  private drawPixelTerminal(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    g.fillStyle(0x0a0f18, 1).fillRect(x - 70, y - 44, 140, 96);
    g.fillStyle(0x172536, 1).fillRect(x - 58, y - 32, 116, 66);
    g.lineStyle(3, 0x1cd8ff, alpha).strokeRect(x - 58, y - 32, 116, 66);
    g.fillStyle(0x9fffee, alpha).fillRect(x - 36, y - 14, 72, 6);
    g.fillStyle(0xffdd77, alpha * 0.75).fillRect(x - 36, y + 8, 48, 5);
  }

  private drawPixelDoor(g: Phaser.GameObjects.Graphics, x: number, y: number, open: number) {
    g.fillStyle(0x05070b, 1).fillRect(x - 22, y, 120, 310);
    g.fillStyle(0x0e1722, 1).fillRect(x, y + 22, 72 - open * 50, 288);
    g.fillStyle(0xffc75c, 0.44 * open).fillRect(x + 72 - open * 46, y + 22, 18 + open * 52, 288);
  }

  private drawImageCover(key: string, alpha: number) {
    const image = this.add.image(WIDTH / 2, HEIGHT / 2, key);
    const scale = Math.max(WIDTH / image.width, HEIGHT / image.height);
    image.setScale(scale);
    image.setAlpha(alpha);
    image.setDepth(-20);
  }

  private drawMemoryWorld(g: Phaser.GameObjects.Graphics, intensity: number) {
    g.fillStyle(0x04101b, 0.28).fillRect(0, 0, WIDTH, HEIGHT);
    for (let i = 0; i < 35; i++) {
      const x = (i * 109 + this.elapsed * 18) % (WIDTH + 120) - 80;
      const y = 60 + ((i * 53) % 420);
      const w = 70 + (i % 5) * 34;
      g.lineStyle(1, i % 4 === 0 ? 0xff477d : 0x5dfaff, intensity * (0.1 + (i % 4) * 0.05));
      g.strokeRect(x, y, w, 12);
    }
  }

  private drawGround(g: Phaser.GameObjects.Graphics, color: number) {
    g.fillStyle(color, 1).fillRect(0, FLOOR_Y, WIDTH, HEIGHT - FLOOR_Y);
    g.lineStyle(2, 0x67f4ff, 0.42).lineBetween(0, FLOOR_Y, WIDTH, FLOOR_Y);
    g.lineStyle(1, 0x67f4ff, 0.16);
    for (let x = -80; x < WIDTH; x += 64) g.lineBetween(x, FLOOR_Y, x + 34, HEIGHT);
  }

  private drawRain(g: Phaser.GameObjects.Graphics, alpha: number) {
    g.lineStyle(1, 0x86f8ff, 0.16 * alpha);
    for (let i = 0; i < 80; i++) {
      const x = (i * 73 + this.elapsed * 220) % WIDTH;
      const y = (i * 41 + this.elapsed * 520) % HEIGHT;
      g.lineBetween(x, y, x - 14, y + 34);
    }
  }

  private drawPlayerSilhouette(g: Phaser.GameObjects.Graphics, x: number, y: number, scale: number, alpha: number) {
    g.fillStyle(0x06080e, alpha).fillRect(x - 18 * scale, y + 42 * scale, 48 * scale, 72 * scale);
    g.fillStyle(0x0e1624, alpha).fillRect(x - 9 * scale, y + 16 * scale, 30 * scale, 28 * scale);
    g.fillStyle(0xdde9ff, alpha * 0.88).fillRect(x - 14 * scale, y + 54 * scale, 34 * scale, 52 * scale);
    g.fillStyle(0x64f1ff, alpha).fillRect(x + 13 * scale, y + 26 * scale, 7 * scale, 4 * scale);
    g.fillStyle(0x111827, alpha).fillRect(x - 9 * scale, y + 18 * scale, 30 * scale, 24 * scale);
  }

  private drawEnemy(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    g.fillStyle(0xff477d, alpha).fillRect(x - 24, y - 52, 48, 52);
    g.fillStyle(0xffb2c8, alpha).fillRect(x - 15, y - 66, 30, 18);
    g.lineStyle(2, 0xff477d, alpha * 0.6).strokeRect(x - 30, y - 72, 60, 76);
  }

  private drawDrone(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    g.fillStyle(0x66efff, alpha).fillCircle(x, y, 24);
    g.fillStyle(0x03101a, alpha).fillCircle(x, y, 9);
    g.lineStyle(2, 0xff477d, alpha * 0.55).lineBetween(x, y, x - 220, FLOOR_Y - 82);
  }

  private drawSlash(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    g.fillStyle(0xffffff, 0.86 * alpha).fillTriangle(x - 20, y + 16, x + 196, y - 70, x + 80, y + 46);
    g.fillStyle(0x65f4ff, 0.56 * alpha).fillTriangle(x - 12, y + 30, x + 166, y - 28, x + 56, y + 58);
    g.fillStyle(0xff3f91, 0.38 * alpha).fillTriangle(x + 22, y + 34, x + 214, y - 18, x + 96, y + 62);
    g.lineStyle(4, 0xffffff, 0.7 * alpha).lineBetween(x - 12, y + 20, x + 178, y - 54);
  }

  private drawSpeedLines(g: Phaser.GameObjects.Graphics, x: number, y: number, p: number) {
    const active = p > 0.12 && p < 0.68 ? 1 : 0.35;
    for (let i = 0; i < 12; i++) {
      const yy = y + ((i * 17 + this.elapsed * 180) % 120) - 70;
      const xx = x - 280 - i * 38;
      g.fillStyle(i % 2 ? 0xff3f91 : 0x67f4ff, active * (0.08 + i * 0.006)).fillRect(xx, yy, 140 + i * 8, 3);
    }
  }

  private drawHitBurst(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    const a = Phaser.Math.Clamp(alpha, 0, 1);
    for (let i = 0; i < 12; i++) {
      const angle = (i / 12) * Math.PI * 2;
      const len = 24 + (i % 3) * 18;
      g.lineStyle(3, i % 2 ? 0xff3f91 : 0xffffff, 0.75 * a);
      g.lineBetween(x, y, x + Math.cos(angle) * len, y + Math.sin(angle) * len);
    }
  }

  private drawNeonSign(g: Phaser.GameObjects.Graphics, x: number, y: number, w: number, h: number, color: number, text: string, size: number) {
    g.fillStyle(0x06111a, 0.72).fillRect(x, y, w, h);
    g.lineStyle(2, color, 0.7 + Math.sin(this.elapsed * 5) * 0.2).strokeRect(x, y, w, h);
    const title = this.add.text(x + w / 2, y + h / 2, text, {
      fontFamily: "Segoe UI, Pretendard, sans-serif",
      fontSize: `${size}px`,
      color: "#e9fbff",
      fontStyle: "700",
    }).setOrigin(0.5);
    title.setShadow(0, 0, "#5dfaff", 20, true, true);
  }

  private drawSmallNeon(g: Phaser.GameObjects.Graphics, x: number, y: number, text: string, color: number) {
    this.add.text(x, y, text, {
      fontFamily: "Segoe UI, Pretendard, sans-serif",
      fontSize: "18px",
      color: `#${color.toString(16).padStart(6, "0")}`,
    }).setShadow(0, 0, "#5dfaff", 10, true, true);
  }

  private drawSystemPanel(g: Phaser.GameObjects.Graphics, x: number, y: number, text: string, alpha: number) {
    g.fillStyle(0x03070d, 0.72 * alpha).fillRect(x, y, 500, 166);
    g.lineStyle(2, 0x65f4ff, 0.42 * alpha).strokeRect(x, y, 500, 166);
    this.add.text(x + 24, y + 22, text, {
      fontFamily: "Consolas, 'Segoe UI', monospace",
      fontSize: "22px",
      lineSpacing: 9,
      color: "#cffff8",
    });
  }

  private drawHouseMemory(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    g.fillStyle(0xffd66b, alpha * 0.18).fillRect(x, y + 90, 220, 120);
    g.lineStyle(2, 0xffd66b, alpha).strokeRect(x, y + 90, 220, 120);
    g.lineStyle(2, 0xffd66b, alpha);
    g.lineBetween(x - 12, y + 90, x + 110, y);
    g.lineBetween(x + 110, y, x + 232, y + 90);
  }

  private drawTableMemory(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    g.lineStyle(3, 0xffd66b, alpha).strokeRect(x, y, 180, 30);
    g.lineBetween(x + 24, y + 30, x + 10, y + 94);
    g.lineBetween(x + 156, y + 30, x + 170, y + 94);
  }

  private drawDoorMemory(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    g.lineStyle(3, 0xffd66b, alpha).strokeRect(x, y, 96, 178);
    g.fillStyle(0xffd66b, alpha).fillCircle(x + 72, y + 92, 5);
  }

  private drawResidualFigure(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    for (let i = 0; i < 5; i++) {
      g.lineStyle(3, i % 2 ? 0xff477d : 0x7ffff5, alpha * (0.26 - i * 0.03));
      g.strokeCircle(x + Math.sin(this.elapsed * 9 + i) * 9, y + i * 28, 48 - i * 4);
    }
    g.fillStyle(0xffffff, alpha * 0.72).fillRect(x - 22, y + 72, 44, 96);
    g.fillStyle(0x03070d, alpha).fillRect(x - 14, y + 88, 28, 68);
  }

  private drawResidualSprite(x: number, y: number, scale: number, alpha: number) {
    const sprite = this.add.image(x, y, "spr-residual-guardian");
    sprite.setScale(scale);
    sprite.setAlpha(alpha);
    sprite.setDepth(-2);
    sprite.setBlendMode(Phaser.BlendModes.SCREEN);
    const ghost = this.add.image(x + Math.sin(this.elapsed * 20) * 9, y, "spr-residual-guardian");
    ghost.setScale(scale * 1.02);
    ghost.setAlpha(0.18);
    ghost.setTint(0x65f4ff);
    ghost.setDepth(-1);
    ghost.setBlendMode(Phaser.BlendModes.ADD);
  }

  private drawTerminal(g: Phaser.GameObjects.Graphics, x: number, y: number, alpha: number) {
    g.fillStyle(0x202837, 1).fillRect(x - 58, y - 42, 118, 102);
    g.lineStyle(2, 0x65f4ff, alpha).strokeRect(x - 46, y - 30, 94, 58);
    g.fillStyle(0xa8fff4, alpha).fillRect(x - 30, y - 16, 62, 6);
    g.fillStyle(0xffe47a, alpha * 0.6).fillRect(x - 30, y + 2, 46, 5);
  }

  private drawDebtPanel(g: Phaser.GameObjects.Graphics, p: number) {
    const text = p > 0.45 ? "[채무 잔액]\n83,420C\n[최근 의뢰]\nHOME 회수 실패" : "[회수 실패]\n파일 일부만 확보";
    this.drawSystemPanel(g, 94, 70, text, 0.46);
  }

  private drawDoor(g: Phaser.GameObjects.Graphics, x: number, y: number, open: number) {
    g.fillStyle(0x0d151e, 1).fillRect(x - 30, y, 128, 318);
    g.fillStyle(0x111f2a, 1).fillRect(x, y + 22, 78 - open * 52, 296);
    g.fillStyle(0xffd66b, 0.4 * open).fillRect(x + 80 - open * 40, y + 22, 10 + open * 38, 296);
  }

  private drawOverlay(g: Phaser.GameObjects.Graphics) {
    g.lineStyle(1, 0xffffff, 0.04);
    for (let y = -8 + (this.scanOffset % 8); y < HEIGHT; y += 8) g.lineBetween(0, y, WIDTH, y);
    if (this.glitch > 0) {
      const a = Phaser.Math.Clamp(this.glitch * 1.8, 0, 0.55);
      for (let i = 0; i < 18; i++) {
        const y = (i * 41 + this.elapsed * 900) % HEIGHT;
        g.fillStyle(i % 2 ? 0xff477d : 0x65f4ff, a * 0.5).fillRect((i % 3 - 1) * 16, y, WIDTH, 4 + (i % 4) * 5);
      }
    }
    if (this.flash > 0) g.fillStyle(0xffffff, Phaser.Math.Clamp(this.flash * 2.6, 0, 0.56)).fillRect(0, 0, WIDTH, HEIGHT);
    g.lineStyle(2, 0x65f4ff, 0.45).strokeRect(0, 0, WIDTH, HEIGHT);
  }

  private drawReelText(scene: ReelScene) {
    this.add.text(24, 20, `${scene.title}  /  ${scene.check}`, {
      fontFamily: "Segoe UI, Pretendard, sans-serif",
      fontSize: "17px",
      color: "#b8fff4",
    }).setShadow(0, 0, "#000000", 8, true, true);
    this.add.text(24, 50, "1-7 장면 이동   ←/→ 이전/다음   SPACE 자동재생   F 전체화면", {
      fontFamily: "Segoe UI, Pretendard, sans-serif",
      fontSize: "14px",
      color: "#8298a8",
    });
    this.add.text(WIDTH - 210, 22, `${this.autoPlay ? "AUTO" : "PAUSED"} ${(this.elapsed / scene.duration * 100).toFixed(0)}%`, {
      fontFamily: "Consolas, monospace",
      fontSize: "15px",
      color: "#ffec9a",
    });
  }

  private updateDom() {
    const scene = this.current();
    this.setDomText("objective", "");
    this.setDomText("status", "");
    const prompt = document.getElementById("prompt");
    if (prompt) prompt.classList.add("hidden");

    const box = document.getElementById("dialogue");
    if (!box) return;
    if (!this.activeCue) {
      box.classList.add("hidden");
      return;
    }
    box.classList.remove("hidden");
    this.setDomText("speaker", this.activeCue.speaker);
    this.setDomText("line", this.activeCue.text);
    if (scene.id === "failure") this.glitch = Math.max(this.glitch, 0.22);
  }

  private setDomText(id: string, text: string) {
    const el = document.getElementById(id);
    if (el) el.textContent = text;
  }

  private computeStateText() {
    const scene = this.current();
    return JSON.stringify({
      coordinateSystem: "origin top-left, x right, y down",
      mode: "visual-reel",
      scene: { index: this.sceneIndex + 1, id: scene.id, title: scene.title, check: scene.check },
      elapsed: Number(this.elapsed.toFixed(2)),
      duration: scene.duration,
      autoPlay: this.autoPlay,
      cue: this.activeCue ? { speaker: this.activeCue.speaker, text: this.activeCue.text } : null,
      effects: {
        glitch: Number(this.glitch.toFixed(2)),
        flash: Number(this.flash.toFixed(2)),
        shake: Number(this.shake.toFixed(2)),
      },
    });
  }

  private installTestHooks() {
    const win = window as typeof window & {
      render_game_to_text?: () => string;
      __deepDiveVisualReel?: {
        goto: (index: number) => void;
        pause: () => void;
      };
    };
    win.render_game_to_text = () => this.lastStateText || this.computeStateText();
    win.__deepDiveVisualReel = {
      goto: (index: number) => this.gotoScene(index - 1),
      pause: () => {
        this.autoPlay = false;
      },
    };
  }
}

new Phaser.Game({
  type: Phaser.CANVAS,
  parent: "game-root",
  width: WIDTH,
  height: HEIGHT,
  backgroundColor: "#03050a",
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH,
  },
  scene: VisualReelScene,
  render: {
    antialias: false,
    pixelArt: true,
  },
});
