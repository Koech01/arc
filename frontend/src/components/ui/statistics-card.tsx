import { cn } from '@/lib/utils';
import { Badge } from "@/components/ui/badge";
import { TrendingDownIcon, TrendingUpIcon } from "lucide-react"; 
import { Card, CardTitle, CardContent, CardDescription, CardFooter } from '@/components/ui/card';


// Statistics card data type
type StatisticsCardProps = {
  value: string
  title: string
  changePercentage: string
  trend: 'up' | 'down'
  sentiment: 'positive' | 'negative'
  footerPrimary: string
  footerSecondary?: string
  className?: string
}

const StatisticsCard = ({ value, title, changePercentage, trend, sentiment, footerPrimary, footerSecondary, className }: StatisticsCardProps) => {
  const isZeroChange = changePercentage === '+0.0 %' || changePercentage === '0.0 %' || changePercentage === '+0 %' || changePercentage === '0 %';
 
  return (
    <Card className={cn('gap-3 p-3', className)}>  
      <CardContent className='flex flex-col gap-3 p-0'>

        <div className='flex items-center justify-between'>
          <CardDescription className='m-0'>{title}</CardDescription>

          <Badge variant="outline" className={cn("flex gap-1 rounded-lg text-xs m-0", isZeroChange ? '' : '', !isZeroChange && (sentiment === 'positive' ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'))}>
            {!isZeroChange && (trend === 'up' ? <TrendingUpIcon className="size-3 m-0" /> : <TrendingDownIcon className="size-3 m-0" />)}
            {changePercentage}
          </Badge>
        </div>

          <CardTitle className="@[250px]/card:text-3xl text-2xl font-semibold tabular-nums">{value}</CardTitle>
      </CardContent>

      <CardFooter className="flex-col items-start gap-3 text-sm p-0 border-0 bg-transparent">
        <p className='space-x-2 m-0 mt-3'>
          <span className='text-sm'>{footerPrimary}</span>
        </p> 
        {footerSecondary && <div className="text-muted-foreground">{footerSecondary}</div>}
      </CardFooter>
    </Card>
  )
}

export default StatisticsCard