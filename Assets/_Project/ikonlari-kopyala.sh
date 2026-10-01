#!/bin/bash
# Hayal Garajı: Fluent Emoji 3D setinden oyunun ihtiyaç duyduğu ikonları kopyalar.
# Kullanım:
#   bash ikonlari-kopyala.sh <fluentui-emoji klasörü> <HayalGaraji proje klasörü>
# Örnek:
#   bash ikonlari-kopyala.sh ~/Downloads/fluentui-emoji-main ~/Unity/HayalGaraji

SRC="${1%/}/assets"
DST="${2%/}/Assets/_Project/Art/Icons"

if [ ! -d "$SRC" ]; then echo "Bulunamadı: $SRC  (fluentui-emoji klasörünü doğru verdiğinden emin ol)"; exit 1; fi
if [ ! -d "${2%/}/Assets" ]; then echo "Bulunamadı: ${2%/}/Assets  (Unity proje klasörünü doğru verdiğinden emin ol)"; exit 1; fi
mkdir -p "$DST"

ok=0; miss=0
while IFS='|' read -r key name; do
  [ -z "$key" ] && continue
  dir=$(find "$SRC" -maxdepth 1 -type d -iname "$name" 2>/dev/null | head -1)
  f=""
  if [ -n "$dir" ]; then
    f=$(find "$dir" -path "*3D*" -name "*.png" 2>/dev/null | grep -iv "dark\|medium\|light" | head -1)
    [ -z "$f" ] && f=$(find "$dir" -path "*3D*" -name "*.png" 2>/dev/null | head -1)
  fi
  if [ -n "$f" ]; then cp "$f" "$DST/$key.png"; echo "✓ $key  ←  $name"; ok=$((ok+1))
  else echo "✗ BULUNAMADI: $name  ($key)"; miss=$((miss+1)); fi
done <<'LIST'
cat_paint|Artist palette
cat_brush|Paintbrush
cat_stickers|Glowing star
cat_wheels|Wheel
cat_face|Smiling face with smiling eyes
cat_buddy|Cat face
cat_sound|Postal horn
surprise|Wrapped gift
photo|Camera with flash
vroom|Dashing away
cat_exhaust|Dashing away
engine_key|Key
night|Crescent moon
day|Sun
none|Cross mark
target_body|Automobile
target_roof|Top hat
target_rim|Wheel
finish_solid|Blue circle
finish_rainbow|Rainbow
finish_twotone|Lollipop
finish_neon|Light bulb
brush_thin|Pencil
brush_thick|Crayon
rainbow|Rainbow
glitter|Sparkles
sponge|Sponge
small|Mouse face
medium|Dog face
big|Elephant
low|Turtle
height_normal|Automobile
high|Giraffe
giant|Tokyo tower
hop|Kangaroo
inside|Door
outside|Automobile
horn|Postal horn
cat_accessories|Ribbon
album|Framed picture
arrow_up|Up arrow
arrow_down|Down arrow
plus|Plus
minus|Minus
face_normal|Eyes
face_star|Star-struck
face_heart|Smiling face with heart-eyes
part_horn_cow|Cow face
part_horn_duck|Duck
part_horn_car|Automobile
part_horn_cat|Cat
part_horn_dog|Dog
part_horn_sheep|Ewe
part_horn_chicken|Chicken
part_horn_frog|Frog
part_horn_lion|Lion
part_horn_bell|Bell
part_horn_clown|Clown face
sticker_star|Glowing star
sticker_heart|Red heart
sticker_rainbow|Rainbow
sticker_unicorn|Unicorn
sticker_dino|T-Rex
sticker_cat|Cat face
sticker_rocket|Rocket
sticker_blossom|Cherry blossom
sticker_donut|Doughnut
sticker_strawberry|Strawberry
sticker_butterfly|Butterfly
sticker_zap|High voltage
sticker_panda|Panda
sticker_ghost|Ghost
sticker_icecream|Soft ice cream
sticker_crown|Crown
LIST

[ -f "${1%/}/LICENSE" ] && cp "${1%/}/LICENSE" "$DST/LICENSE-FluentEmoji.txt"
echo ""
echo "Bitti: $ok ikon kopyalandı, $miss eksik."
[ $miss -gt 0 ] && echo "Eksik olanları bana yaz, yerine başka bir emoji seçelim."
