#!/usr/bin/env python3
"""Read-only P000-C01 archive self-check. Uses only Python's standard library."""
import argparse
import hashlib
from pathlib import Path
from zipfile import ZipFile

CLASSIC_SHA256 = '4dc2f39f6f129ac165c726291f79307d6f96a39fcf4ecf659a989358e8c1caca'
MODERN_SHA256 = 'fce957aab0785add653c084757a1ff8f7d7faa504b41f3deec698050480d3887'


def sha256_file(path):
    with open(path, 'rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def run(classic, modern):
    successes = []
    failures = []

    def check(name, value):
        (successes if value else failures).append(name)
        print(('PASS' if value else 'FAIL') + ': ' + name)

    check('classic SHA-256', sha256_file(classic) == CLASSIC_SHA256)
    check('modern SHA-256', sha256_file(modern) == MODERN_SHA256)
    with ZipFile(classic) as old, ZipFile(modern) as new:
        check('classic ZIP CRC', old.testzip() is None)
        check('modern ZIP CRC', new.testzip() is None)
        check('classic file count', len(old.namelist()) == 63)
        check('modern file count', len(new.namelist()) == 908)
        check('classic archive size', classic.stat().st_size == 56674412)
        for item in ('bin/friendsui.dll', 'bin/vgui2_s.dll', 'bin/gameoverlayui.dll', 'GameOverlayRenderer64.dll'):
            check('classic archive contains ' + item, item in old.namelist())
        for path, token in (
            ('bin/friendsui.dll', b'CAchievementNotification'),
            ('bin/friendsui.dll', b'friends/AchievementNotification.res'),
            ('bin/friendsui.dll', b'AchievementPopup.TitleColor'),
            ('bin/vgui2_s.dll', b'CNotificationManager'),
            ('bin/vgui2_s.dll', b'Notifications.FadeInTime'),
            ('bin/vgui2_s.dll', b'Notifications.StackSize'),
            ('bin/gameoverlayui.dll', b'overlay_achievement_item.layout'),
        ):
            check('classic binary evidence ' + token.decode(), token in old.read(path))
        for item in ('steamui/sp.js', 'steamui/css/sp.css', 'steamui/index.html',
                     'steamui/changelist.txt', 'steamui/skin.css',
                     'steamui/sounds/deck_ui_achievement_toast.wav',
                     'steamui/sounds/desktop_toast_default.wav'):
            check('modern archive contains ' + item, item in new.namelist())
        js = new.read('steamui/sp.js').decode('utf-8')
        css = new.read('steamui/css/sp.css').decode('utf-8')
        skin = new.read('steamui/skin.css').decode('utf-8')
        check('modern UI build stamp', new.read('steamui/changelist.txt').strip() == b'11229305')
        check('modern HTML build stamp', 'CLSTAMP="11229305"' in new.read('steamui/index.html').decode('utf-8'))
        for label, token in (
            ('desktop motion constants', 'const N=0,K=300,V=300,X=0,he=283'),
            ('desktop capacity', 'St=ct?1:3'),
            ('default duration', 'G=5*1e3'),
            ('achievement registry', 'achievement_notification_toast'),
            ('achievement sound identifier', 'ToastAchievement'),
            ('desktop popup movement', 'SteamClient.Window.MoveTo('),
            ('desktop popup visibility', 'SteamClient.Window.ShowWindow()'),
            ('notification FIFO', 'm_rgNotificationToasts.shift()'),
            ('achievement sound routing', 'case Ze.PN.ToastAchievement:return sr?Se.default:It.default'),
        ):
            check('modern JS ' + label, token in js)
        check('modern background CSS', '#23262E' in css and '#0e141b' in css)
        check('modern 283px toast wrapper CSS', 'width:283px;height:70px' in css)
        check('no direct desktop popup classname override in skin.css',
              '_1OOQPJx0nCNP9ME9toCmV7' not in skin)
        check('custom skin present', len(skin) > 500000)
    print(f'RESULT: {len(successes)}/{len(successes) + len(failures)} checks PASS')
    return 0 if not failures else 1


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('--classic', required=True, type=Path)
    p.add_argument('--modern', required=True, type=Path)
    opts = p.parse_args()
    raise SystemExit(run(opts.classic, opts.modern))
