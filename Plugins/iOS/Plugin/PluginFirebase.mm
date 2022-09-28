#import "PluginListener.h"
#import "Firebase.h"

@interface PluginFirebase : NSObject <PluginListener>
@end

REGISTER_PLUGIN_LISTENER(PluginFirebase)

@implementation PluginFirebase

- (void)didFinishLaunching:(NSNotification*)notification
{
    [FIRApp configure];
}

@end
